
using InfinityCodexWebApp;
using InfinityCodexWebApp.Authorization;
using InfinityCodexWebApp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

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
// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "InfinityCodex.Auth";
        if (builder.Environment.IsDevelopment())
        {
            // These are temporary, eventually when its hosted on the same domain, it won't need to be cross-site and can be set to Strict or Lax
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true; 
        }        
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/auth"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/auth"))
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

// Use CORS policy
app.UseCors("AllowAngularDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.MapControllers();

app.Run();
