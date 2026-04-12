# InfinityCodexWebApp

ASP.NET Core Web API sample.

## Run

```bash
dotnet restore
dotnet run
```

Navigate to `https://localhost:7042/swagger` or `http://localhost:5042/swagger`.

## Local Database

The default local configuration uses SQLite with `Data Source=app.db`.

- `app.db` is a local runtime database and is intentionally ignored by git.
- Do not commit local database files or SQLite sidecar files such as `*.db-wal` and `*.db-shm`.
- Commit schema changes through Entity Framework migrations instead of committing a database file.

Common commands:

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

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

## Auth Cookie Settings

The login session cookie is now explicitly configured through `AuthCookie` settings:

```json
"AuthCookie": {
	"LifetimeHours": 168,
	"SlidingExpiration": false
}
```

- `LifetimeHours` controls how long the persistent auth cookie remains valid.
- `SlidingExpiration` controls whether active use extends that window.

The current default is a fixed 7-day session with sliding expiration disabled so the session does not silently extend past its intended lifetime.