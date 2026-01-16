
using InfinityCodexWebApp;
using InfinityCodexWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.MapControllers();

app.Run();
