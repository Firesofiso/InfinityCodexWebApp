using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
