
using InfinityCodexWebApp;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using InfinityCodexWebApp.Integrations.Horizon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var frontendBaseUrl = builder.Configuration.GetValue<string>("Frontend:BaseUrl") ?? "http://localhost:4200/";
var frontendOrigin = GetOrigin(frontendBaseUrl);
var authCookieLifetimeHours = builder.Configuration.GetValue<int?>("AuthCookie:LifetimeHours") ?? 168;
var authCookieSlidingExpiration = builder.Configuration.GetValue<bool?>("AuthCookie:SlidingExpiration") ?? false;

if (authCookieLifetimeHours <= 0)
{
    throw new InvalidOperationException("AuthCookie:LifetimeHours must be greater than zero.");
}

var authCookieLifetime = TimeSpan.FromHours(authCookieLifetimeHours);

var dbProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (dbProvider.Trim().ToLowerInvariant())
    {
        case "sqlite":
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
            break;
        default:
            throw new InvalidOperationException($"Unsupported database provider: {dbProvider}");
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<HorizonApiOptions>(builder.Configuration.GetSection(HorizonApiOptions.SectionName));
builder.Services.AddHttpClient<IHorizonCharacterLookupClient, HorizonCharacterLookupClient>((services, client) =>
{
    var options = services
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<HorizonApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
// Keep the frontend origin configurable so local Angular dev and deployed UI can both call the API with cookies.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            if (!string.IsNullOrWhiteSpace(frontendOrigin))
            {
                policy.WithOrigins(frontendOrigin);
            }

            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "InfinityCodex.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = authCookieLifetime;
        // Keep the session window fixed unless config explicitly enables sliding expiration.
        options.SlidingExpiration = authCookieSlidingExpiration;
        if (builder.Environment.IsDevelopment())
        {
            // During split frontend/backend development the auth cookie must be allowed cross-site for browser redirects and XHR.
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }        
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // API callers expect status codes instead of HTML login redirects.
                if (context.Request.Path.StartsWithSegments("/auth") || context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                // Match the same API-friendly behavior for authorization failures.
                if (context.Request.Path.StartsWithSegments("/auth") || context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddRoleBasedPolicies();
});

var app = builder.Build();

// Run migrations and enable WAL mode on every startup.
// WAL mode allows reads and writes to proceed concurrently without blocking each other.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Trust X-Forwarded-For and X-Forwarded-Proto from nginx so the app sees the real
// client IP and correct scheme even though it only receives plain HTTP inside Docker.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Apply CORS before auth so browser preflight and credentialed requests succeed consistently.
app.UseCors("AllowAngularDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Only redirect to HTTPS in development — nginx handles this in production.
    app.UseHttpsRedirection();
}

// Serve the Angular SPA from wwwroot in production. In development the Angular CLI
// dev server runs separately and is proxied, so these are effectively no-ops there.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.MapControllers();

// Any route not matched by a controller falls back to index.html so Angular's
// client-side router can take over.
app.MapFallbackToFile("index.html");

app.Run();

static string? GetOrigin(string? url)
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return null;
    }

    return Uri.TryCreate(url, UriKind.Absolute, out var uri)
        ? uri.GetLeftPart(UriPartial.Authority)
        : null;
}
