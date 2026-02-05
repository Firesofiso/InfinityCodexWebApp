using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

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

    public AuthController(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
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

        var resolvedState = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state;
        PersistOauthState(resolvedState, returnUrl);
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

            return Problem(message, statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Problem("Discord OAuth code is missing.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryValidateOauthState(state, out var returnUrl))
        {
            return Problem("Discord OAuth state is invalid.", statusCode: StatusCodes.Status400BadRequest);
        }

        var tokenResponse = await ExchangeCodeForToken(code);
        if (tokenResponse is null)
        {
            return Problem("Failed to exchange Discord OAuth code for token.", statusCode: StatusCodes.Status502BadGateway);
        }

        var userResponse = await FetchDiscordUser(tokenResponse.AccessToken);
        if (userResponse is null)
        {
            return Problem("Failed to fetch Discord user profile.", statusCode: StatusCodes.Status502BadGateway);
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

        var resolvedReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        if (!Url.IsLocalUrl(resolvedReturnUrl))
        {
            resolvedReturnUrl = "/";
        }

        return Redirect(resolvedReturnUrl);
    }

    [HttpGet("/auth/me")]
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
        var token = ResolveAccessToken(accessToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Problem("Discord access token is missing.", statusCode: StatusCodes.Status400BadRequest);
        }

        var clientId = _configuration["DiscordOAuth:ClientId"];
        var clientSecret = _configuration["DiscordOAuth:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Problem("Discord OAuth client credentials are missing.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var resolvedTokenTypeHint = string.IsNullOrWhiteSpace(tokenTypeHint) ? "access_token" : tokenTypeHint;
        using var revokeRequest = new HttpRequestMessage(HttpMethod.Post, DiscordTokenRevokeUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["token"] = token,
                ["token_type_hint"] = resolvedTokenTypeHint
            })
        };

        using var response = await _httpClient.SendAsync(revokeRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new
            {
                Error = "Discord token revocation failed.",
                Status = (int)response.StatusCode,
                Details = responseBody
            });
        }

        return Ok(new
        {
            Message = "Logged out.",
            TokenTypeHint = resolvedTokenTypeHint,
            Revoked = true
        });
    }
}
