using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;
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
    private const string DiscordTokenRevokeUrl = "https://discord.com/api/oauth2/token/revoke";
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AuthController(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    [HttpGet("login")]
    public IActionResult DiscordLogin([FromQuery] string? state = null)
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
    public IActionResult DiscordCallback(
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

        return Ok(new
        {
            Code = code,
            State = state
        });
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
