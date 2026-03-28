using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InfinityCodexWebApp.Controllers;

[ApiController]
[Route("auth/discord")]
public class AuthController : ControllerBase
{
    private const string DiscordAuthorizationUrl = "https://discord.com/oauth2/authorize";
    private const string DiscordUserUrl = "https://discord.com/api/users/@me";
    private const string DiscordTokenUrl = "https://discord.com/api/oauth2/token";
    private const string DiscordTokenRevokeUrl = "https://discord.com/api/oauth2/token/revoke";
    private const string DiscordStateCookieName = "discord_oauth_state";
    private const string DiscordReturnUrlCookieName = "discord_oauth_return_url";
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, HttpClient httpClient, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult DiscordLogin([FromQuery] string? state = null, [FromQuery] string? returnUrl = null)
    {
        var clientId = _configuration["DiscordOAuth:ClientId"];
        var redirectUri = _configuration["DiscordOAuth:RedirectUri"];
        var scope = _configuration["DiscordOAuth:Scope"] ?? "identify email";
        var prompt = _configuration["DiscordOAuth:Prompt"] ?? "consent";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return Problem("Discord OAuth configuration is missing.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var resolvedReturnUrl = ResolveReturnUrl(returnUrl);
        if (resolvedReturnUrl is null)
        {
            _logger.LogWarning("Rejected Discord login returnUrl '{ReturnUrl}' because it does not match configured frontend origin.", returnUrl);
            return Problem("The provided return URL is not allowed.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Persist state server-side in cookies so the callback can verify the OAuth round-trip came from us.
        var resolvedState = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state;
        PersistOauthState(resolvedState, resolvedReturnUrl);
        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["state"] = resolvedState,
            ["prompt"] = prompt
        };

        var redirectUrl = QueryHelpers.AddQueryString(DiscordAuthorizationUrl, queryParams);
        return Redirect(redirectUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> DiscordCallback(
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null,
        [FromQuery(Name = "error_description")] string? errorDescription = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            var message = string.IsNullOrWhiteSpace(errorDescription)
                ? $"Discord OAuth error: {error}."
                : $"Discord OAuth error: {error}. {errorDescription}";

            _logger.LogWarning("Discord OAuth callback returned provider error {Error}: {ErrorDescription}", error, errorDescription);
            return RedirectWithAuthError("oauth_provider_error", message);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Discord OAuth callback missing authorization code.");
            return RedirectWithAuthError("oauth_missing_code", "Discord OAuth code is missing.");
        }

        if (!TryValidateOauthState(state, out var returnUrl))
        {
            _logger.LogWarning("Discord callback rejected because OAuth state validation failed. Incoming state: '{State}'.", state);
            return RedirectWithAuthError("oauth_invalid_state", "Discord OAuth state is invalid.");
        }

        var tokenResponse = await ExchangeCodeForToken(code);
        if (tokenResponse is null)
        {
            _logger.LogError("Discord OAuth token exchange failed for callback request.");
            return RedirectWithAuthError("oauth_token_exchange_failed", "Failed to exchange Discord OAuth code for token.", returnUrl);
        }

        var userResponse = await FetchDiscordUser(tokenResponse.AccessToken);
        if (userResponse is null)
        {
            _logger.LogError("Discord user profile fetch failed after successful token exchange.");
            return RedirectWithAuthError("oauth_profile_fetch_failed", "Failed to fetch Discord user profile.", returnUrl);
        }

        var claims = BuildClaims(userResponse.Value);
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };
        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
            new AuthenticationToken { Name = "token_type", Value = tokenResponse.TokenType }
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        // After the cookie is issued, send the browser back to the frontend page that initiated login.
        return Redirect(ResolveReturnUrl(returnUrl) ?? GetFrontendBaseUrl());
    }

    

    [HttpGet("me")]
    public async Task<IActionResult> DiscordMe([FromQuery(Name = "access_token")] string? accessToken = null)
    {
        var token = ResolveAccessToken(accessToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Problem("Discord access token is missing.", statusCode: StatusCodes.Status401Unauthorized);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, DiscordUserUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new
            {
                Error = "Discord API request failed.",
                Status = (int)response.StatusCode,
                Details = responseBody
            });
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return Ok();
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return Ok(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            return Content(responseBody, contentType);
        }
    }

    [AllowAnonymous]
    [HttpGet("session")]
    public IActionResult Session()
    {
        var user = HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            // Treat "not logged in" as normal state so the SPA does not have to special-case 401 handling.
            return Ok(new
            {
                IsAuthenticated = false
            });
        }

        return Ok(new
        {
            IsAuthenticated = true,
            Name = user.Identity?.Name,
            Claims = user.Claims.Select(claim => new
            {
                claim.Type,
                claim.Value
            })
        });
    }


    private string? ResolveAccessToken(string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var headerValue = authHeader.ToString();
        const string bearerPrefix = "Bearer ";
        if (headerValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return headerValue[bearerPrefix.Length..].Trim();
        }

        return null;
    }

    private void PersistOauthState(string state, string? returnUrl)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        };

        Response.Cookies.Append(DiscordStateCookieName, state, cookieOptions);

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            Response.Cookies.Append(DiscordReturnUrlCookieName, returnUrl, cookieOptions);
        }
    }

    private bool TryValidateOauthState(string? state, out string? returnUrl)
    {
        returnUrl = null;

        if (string.IsNullOrWhiteSpace(state))
        {
            return false;
        }

        if (!Request.Cookies.TryGetValue(DiscordStateCookieName, out var storedState))
        {
            return false;
        }

        Response.Cookies.Delete(DiscordStateCookieName);

        if (!string.Equals(state, storedState, StringComparison.Ordinal))
        {
            return false;
        }

        if (Request.Cookies.TryGetValue(DiscordReturnUrlCookieName, out var storedReturnUrl))
        {
            Response.Cookies.Delete(DiscordReturnUrlCookieName);
            returnUrl = storedReturnUrl;
        }

        return true;
    }

    private async Task<DiscordTokenResponse?> ExchangeCodeForToken(string code)
    {
        var clientId = _configuration["DiscordOAuth:ClientId"];
        var clientSecret = _configuration["DiscordOAuth:ClientSecret"];
        var redirectUri = _configuration["DiscordOAuth:RedirectUri"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return null;
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, DiscordTokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            })
        };

        using var response = await _httpClient.SendAsync(tokenRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var accessToken = root.GetProperty("access_token").GetString();
            var tokenType = root.GetProperty("token_type").GetString() ?? "Bearer";

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            return new DiscordTokenResponse(accessToken, tokenType);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<JsonElement?> FetchDiscordUser(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, DiscordUserUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<Claim> BuildClaims(JsonElement userProfile)
    {
        var claims = new List<Claim>();

        if (userProfile.TryGetProperty("id", out var idValue))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, idValue.GetString() ?? string.Empty));
        }

        if (userProfile.TryGetProperty("username", out var usernameValue))
        {
            claims.Add(new Claim(ClaimTypes.Name, usernameValue.GetString() ?? string.Empty));
        }

        if (userProfile.TryGetProperty("email", out var emailValue))
        {
            claims.Add(new Claim(ClaimTypes.Email, emailValue.GetString() ?? string.Empty));
        }

        return claims;
    }

    private sealed record DiscordTokenResponse(string AccessToken, string TokenType);

    [HttpPost("/auth/logout")]
    public async Task<IActionResult> DiscordLogout(
        [FromQuery(Name = "access_token")] string? accessToken = null,
        [FromQuery(Name = "token_type_hint")] string? tokenTypeHint = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Prefer the token stored in the auth cookie so the frontend can log out without tracking Discord tokens itself.
        var token = ResolveAccessToken(accessToken) ?? authenticateResult.Properties?.GetTokenValue("access_token");
        var resolvedTokenTypeHint = string.IsNullOrWhiteSpace(tokenTypeHint) ? "access_token" : tokenTypeHint;
        var revoked = false;

        if (!string.IsNullOrWhiteSpace(token))
        {
            var clientId = _configuration["DiscordOAuth:ClientId"];
            var clientSecret = _configuration["DiscordOAuth:ClientSecret"];

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                revoked = await RevokeDiscordToken(token, resolvedTokenTypeHint, clientId, clientSecret);
            }
            else
            {
                _logger.LogWarning("Skipping Discord token revocation because client credentials are missing.");
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Clear transient OAuth cookies as part of the same logout path.
        Response.Cookies.Delete(DiscordStateCookieName);
        Response.Cookies.Delete(DiscordReturnUrlCookieName);

        return Ok(new
        {
            Message = "Logged out.",
            TokenTypeHint = resolvedTokenTypeHint,
            Revoked = revoked,
            SessionCleared = true
        });
    }

    private string GetFrontendBaseUrl()
    {
        return _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200/";
    }

    private string? ResolveReturnUrl(string? returnUrl)
    {
        var configuredFrontendUrl = GetFrontendBaseUrl();
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return configuredFrontendUrl;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var candidateUri))
        {
            return null;
        }

        if (!Uri.TryCreate(configuredFrontendUrl, UriKind.Absolute, out var frontendUri))
        {
            return null;
        }

        var sameOrigin = string.Equals(candidateUri.Scheme, frontendUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidateUri.Host, frontendUri.Host, StringComparison.OrdinalIgnoreCase)
            && candidateUri.Port == frontendUri.Port;

        // Only allow redirects back to the configured frontend origin.
        return sameOrigin ? candidateUri.ToString() : null;
    }

    private async Task<bool> RevokeDiscordToken(string token, string tokenTypeHint, string clientId, string clientSecret)
    {
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, DiscordTokenRevokeUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint
            })
        };

        using var response = await _httpClient.SendAsync(revokeRequest);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Discord token revocation failed with status {StatusCode}. Response: {ResponseBody}",
            (int)response.StatusCode,
            responseBody);

        return false;
    }

    private IActionResult RedirectWithAuthError(string authErrorCode, string authErrorMessage, string? returnUrl = null)
    {
        var safeReturnUrl = ResolveReturnUrl(returnUrl) ?? GetFrontendBaseUrl();
        var redirectUrl = QueryHelpers.AddQueryString(safeReturnUrl, new Dictionary<string, string?>
        {
            ["authError"] = authErrorCode,
            ["authErrorMessage"] = authErrorMessage
        });

        return Redirect(redirectUrl);
    }
}
