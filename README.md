# InfinityCodexWebApp

ASP.NET Core Web API sample.

## Run

```bash
dotnet restore
dotnet run
```

Navigate to `https://localhost:7042/swagger` or `http://localhost:5042/swagger`.

## Local Secrets

The Discord OAuth client secret is intentionally not stored in tracked config files.

Set it locally with ASP.NET Core user secrets:

```bash
dotnet user-secrets set "DiscordOAuth:ClientSecret" "your-secret-here"
```

You can verify it is present with:

```bash
dotnet user-secrets list
```

For deployed environments, provide the same value through the `DiscordOAuth__ClientSecret` environment variable.
