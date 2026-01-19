using System;
using System.Collections.Generic;
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
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
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
}
