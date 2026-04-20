using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
    private const string DiscordCurrentUserGuildMemberUrlTemplate = "https://discord.com/api/users/@me/guilds/{0}/member";
    private const string DiscordStateCookieName = "discord_oauth_state";
    private const string DiscordReturnUrlCookieName = "discord_oauth_return_url";
    private const string RegistrationStatusClaimType = "infinity:registration_status";
    private const string RegistrationStatusPending = "pending";
    private const string RegistrationStatusComplete = "complete";
    private const string HorizonCharacterDataSource = "horizon-api";

    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        HttpClient httpClient,
        ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult DiscordLogin([FromQuery] string? state = null, [FromQuery] string? returnUrl = null)
    {
        var clientId = _configuration["DiscordOAuth:ClientId"];
        var redirectUri = _configuration["DiscordOAuth:RedirectUri"];
        var scope = _configuration["DiscordOAuth:Scope"] ?? "identify email guilds.members.read";
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

        var userProfile = ExtractDiscordUserProfile(userResponse.Value);
        if (userProfile is null)
        {
            _logger.LogError("Discord user profile payload did not contain an id.");
            return RedirectWithAuthError("oauth_profile_invalid", "Discord profile payload was invalid.", returnUrl);
        }

        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(user => user.DiscordId == userProfile.DiscordId);
        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                return RedirectWithAuthError("account_inactive", "Your registration is currently inactive.", returnUrl);
            }

            if (existingUser.IsRegistrationComplete)
            {
                await SignInUserAsync(existingUser, userProfile, pendingRegistration: false, tokenResponse: tokenResponse);
                return Redirect(ResolveReturnUrl(returnUrl) ?? GetFrontendBaseUrl());
            }

            await SignInUserAsync(existingUser, userProfile, pendingRegistration: true, tokenResponse: tokenResponse);
            return Redirect(GetFrontendRegistrationUrl());
        }

        var guildId = _configuration["DiscordOAuth:RegistrationGuildId"];
        if (string.IsNullOrWhiteSpace(guildId))
        {
            _logger.LogError("DiscordOAuth:RegistrationGuildId is not configured; cannot evaluate registration eligibility.");
            return RedirectWithAuthError("registration_unavailable", "Registration is currently unavailable.", returnUrl);
        }

        var guildMembership = await CheckGuildMembership(tokenResponse.AccessToken, guildId);
        if (!guildMembership.Success)
        {
            _logger.LogWarning("Discord guild membership check failed for registration. Reason: {Reason}", guildMembership.ErrorMessage);
            return RedirectWithAuthError("registration_membership_check_failed", "Could not verify Linkshell membership right now. Please try again.", returnUrl);
        }

        if (!guildMembership.IsMember)
        {
            return RedirectWithAuthError("registration_not_allowed", "You must be in the Linkshell Discord server to register.", returnUrl);
        }

        var pendingUser = new User
        {
            DiscordId = userProfile.DiscordId,
            DisplayName = userProfile.Username,
            Role = AppRoles.Reader,
            IsActive = true,
            IsRegistrationComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(pendingUser);
        await _dbContext.SaveChangesAsync();

        await SignInUserAsync(pendingUser, userProfile, pendingRegistration: true, tokenResponse: tokenResponse);
        return Redirect(GetFrontendRegistrationUrl());
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
            return Ok(new
            {
                IsAuthenticated = false,
                RegistrationStatus = (string?)null,
                CanAccessApp = false
            });
        }

        var registrationStatus = user.FindFirstValue(RegistrationStatusClaimType) ?? RegistrationStatusPending;
        var role = user.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            IsAuthenticated = true,
            Name = user.Identity?.Name,
            RegistrationStatus = registrationStatus,
            IsRegistrationComplete = string.Equals(registrationStatus, RegistrationStatusComplete, StringComparison.OrdinalIgnoreCase),
            CanAccessApp = string.Equals(registrationStatus, RegistrationStatusComplete, StringComparison.OrdinalIgnoreCase),
            Role = role,
            Claims = user.Claims.Select(claim => new
            {
                claim.Type,
                claim.Value
            })
        });
    }

    [Authorize]
    [HttpGet("/auth/registration/context")]
    public async Task<IActionResult> GetRegistrationContext()
    {
        var discordId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Unauthorized(new { Message = "Discord identity is missing from the current session." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(entity => entity.DiscordId == discordId);
        if (user is null)
        {
            return NotFound(new { Message = "Registration record was not found." });
        }

        var characterNames = await _dbContext.Characters
            .Where(character => character.OwnerUserId == user.Id && character.IsActive)
            .OrderBy(character => character.Name)
            .Select(character => character.Name)
            .Take(3)
            .ToListAsync();

        var preferredJobs = await _dbContext.UserPreferredJobs
            .Where(entity => entity.UserId == user.Id)
            .OrderBy(entity => entity.JobCode)
            .Select(entity => entity.JobCode)
            .ToListAsync();

        return Ok(new
        {
            IsRegistrationComplete = user.IsRegistrationComplete,
            DisplayName = user.DisplayName,
            PreferredJobs = preferredJobs,
            CharacterNames = characterNames,
            DiscordName = User.Identity?.Name
        });
    }

    [Authorize]
    [HttpPost("/auth/registration/complete")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { Message = "Registration payload is required." });
        }

        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return BadRequest(new { Message = "Display name is required." });
        }

        if (displayName.Length > 64)
        {
            return BadRequest(new { Message = "Display name must be 64 characters or fewer." });
        }

        var discordId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Unauthorized(new { Message = "Discord identity is missing from the current session." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(entity => entity.DiscordId == discordId);
        if (user is null)
        {
            return NotFound(new { Message = "Registration record was not found." });
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        if (user.IsRegistrationComplete)
        {
            return Ok(new { Message = "Registration is already complete." });
        }

        var normalizedPreferredJobs = NormalizePreferredJobs(request.PreferredJobs);
        var normalizedCharacterNames = NormalizeCharacterNames(request.CharacterNames);

        var existingCharacters = await _dbContext.Characters
            .Where(character => character.OwnerUserId == user.Id && character.DataSource == HorizonCharacterDataSource)
            .ToListAsync();

        var selectedNames = new HashSet<string>(normalizedCharacterNames, StringComparer.OrdinalIgnoreCase);

        foreach (var existingCharacter in existingCharacters)
        {
            if (selectedNames.Contains(existingCharacter.Name))
            {
                existingCharacter.IsActive = true;
                existingCharacter.LastSyncedAt = DateTime.UtcNow;
                continue;
            }

            existingCharacter.IsActive = false;
        }

        var existingNameSet = new HashSet<string>(existingCharacters.Select(character => character.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var characterName in normalizedCharacterNames)
        {
            if (existingNameSet.Contains(characterName))
            {
                continue;
            }

            _dbContext.Characters.Add(new Character
            {
                Name = characterName,
                OwnerUserId = user.Id,
                IsActive = true,
                DataSource = HorizonCharacterDataSource,
                LastSyncedAt = DateTime.UtcNow
            });
        }

        user.DisplayName = displayName;
        user.IsRegistrationComplete = true;
        user.RegistrationCompletedAt = DateTime.UtcNow;

        var existingPreferredJobs = await _dbContext.UserPreferredJobs
            .Where(entity => entity.UserId == user.Id)
            .ToListAsync();

        if (existingPreferredJobs.Count > 0)
        {
            _dbContext.UserPreferredJobs.RemoveRange(existingPreferredJobs);
        }

        foreach (var jobCode in normalizedPreferredJobs)
        {
            _dbContext.UserPreferredJobs.Add(new UserPreferredJob
            {
                UserId = user.Id,
                JobCode = jobCode
            });
        }

        await _dbContext.SaveChangesAsync();

        var currentProfile = new DiscordUserProfile(
            user.DiscordId,
            User.Identity?.Name ?? user.DisplayName,
            User.FindFirstValue(ClaimTypes.Email));

        await SignInUserAsync(user, currentProfile, pendingRegistration: false);

        return Ok(new
        {
            Message = "Registration complete.",
            IsRegistrationComplete = true
        });
    }

    [HttpPost("/auth/logout")]
    public async Task<IActionResult> DiscordLogout(
        [FromQuery(Name = "access_token")] string? accessToken = null,
        [FromQuery(Name = "token_type_hint")] string? tokenTypeHint = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

    private async Task SignInUserAsync(User user, DiscordUserProfile userProfile, bool pendingRegistration, DiscordTokenResponse? tokenResponse = null)
    {
        var claims = BuildClaims(user, userProfile, pendingRegistration).ToList();
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };

        if (tokenResponse is not null)
        {
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                new AuthenticationToken { Name = "token_type", Value = tokenResponse.TokenType }
            });
        }
        else
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var accessToken = authenticateResult.Properties?.GetTokenValue("access_token");
            var tokenType = authenticateResult.Properties?.GetTokenValue("token_type") ?? "Bearer";

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                authProperties.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = accessToken },
                    new AuthenticationToken { Name = "token_type", Value = tokenType }
                });
            }
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private static IEnumerable<Claim> BuildClaims(User user, DiscordUserProfile userProfile, bool pendingRegistration)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userProfile.DiscordId),
            new(ClaimTypes.Name, pendingRegistration ? userProfile.Username : user.DisplayName),
            new(RegistrationStatusClaimType, pendingRegistration ? RegistrationStatusPending : RegistrationStatusComplete)
        };

        if (!string.IsNullOrWhiteSpace(userProfile.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, userProfile.Email));
        }

        if (!pendingRegistration && !string.IsNullOrWhiteSpace(user.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        return claims;
    }

    private static DiscordUserProfile? ExtractDiscordUserProfile(JsonElement userProfile)
    {
        if (!userProfile.TryGetProperty("id", out var idValue))
        {
            return null;
        }

        var discordId = idValue.GetString();
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return null;
        }

        var username = userProfile.TryGetProperty("username", out var usernameValue)
            ? usernameValue.GetString()
            : null;

        var email = userProfile.TryGetProperty("email", out var emailValue)
            ? emailValue.GetString()
            : null;

        return new DiscordUserProfile(discordId, username ?? "Discord User", email);
    }

    private async Task<GuildMembershipResult> CheckGuildMembership(string accessToken, string guildId)
    {
        var membershipUrl = string.Format(DiscordCurrentUserGuildMemberUrlTemplate, WebUtility.UrlEncode(guildId));
        using var request = new HttpRequestMessage(HttpMethod.Get, membershipUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return GuildMembershipResult.Member();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return GuildMembershipResult.NotMember();
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        return GuildMembershipResult.Failure($"Discord membership check failed with {(int)response.StatusCode}. {responseBody}");
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

    private string GetFrontendBaseUrl()
    {
        return _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200/";
    }

    private string GetFrontendRegistrationUrl()
    {
        var frontendBaseUrl = GetFrontendBaseUrl();
        if (!Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var frontendUri))
        {
            return $"{frontendBaseUrl.TrimEnd('/')}/register";
        }

        return new Uri(frontendUri, "register").ToString();
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

    private static List<string> NormalizePreferredJobs(IEnumerable<string>? preferredJobs)
    {
        if (preferredJobs is null)
        {
            return new List<string>();
        }

        return preferredJobs
            .Select(job => job?.Trim())
            .Where(job => !string.IsNullOrWhiteSpace(job))
            .Select(job => job!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(job => job, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private static List<string> NormalizeCharacterNames(IEnumerable<string>? characterNames)
    {
        if (characterNames is null)
        {
            return new List<string>();
        }

        return characterNames
            .Select(characterName => characterName?.Trim())
            .Where(characterName => !string.IsNullOrWhiteSpace(characterName))
            .Select(characterName => characterName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(characterName => characterName, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private sealed record DiscordTokenResponse(string AccessToken, string TokenType);

    private sealed record DiscordUserProfile(string DiscordId, string Username, string? Email);

    private sealed record GuildMembershipResult(bool Success, bool IsMember, string? ErrorMessage)
    {
        public static GuildMembershipResult Member() => new(Success: true, IsMember: true, ErrorMessage: null);

        public static GuildMembershipResult NotMember() => new(Success: true, IsMember: false, ErrorMessage: null);

        public static GuildMembershipResult Failure(string errorMessage) => new(Success: false, IsMember: false, ErrorMessage: errorMessage);
    }

    public sealed class CompleteRegistrationRequest
    {
        public string? DisplayName { get; set; }

        public List<string> PreferredJobs { get; set; } = new();

        public List<string> CharacterNames { get; set; } = new();
    }
}
