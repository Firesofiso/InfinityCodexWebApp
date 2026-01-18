using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterJob> CharacterJobs => Set<CharacterJob>();
    public DbSet<CharacterJobLevel> CharacterJobLevels => Set<CharacterJobLevel>();
    public DbSet<CharacterItem> CharacterItems => Set<CharacterItem>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemAllowedJob> ItemAllowedJobs => Set<ItemAllowedJob>();
    public DbSet<ContentSource> ContentSources => Set<ContentSource>();
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasIndex(character => new { character.OwnerUserId, character.Name })
                .IsUnique();
        });

        modelBuilder.Entity<CharacterJob>(entity =>
        {
            entity.HasIndex(characterJob => new { characterJob.CharacterId, characterJob.JobCode })
                .IsUnique();
        });

        modelBuilder.Entity<CharacterJobLevel>(entity =>
        {
            entity.HasKey(characterJobLevel => new { characterJobLevel.CharacterId, characterJobLevel.JobCode });
        });

        modelBuilder.Entity<CharacterItem>(entity =>
        {
            entity.HasIndex(characterItem => new { characterItem.CharacterId, characterItem.ItemId })
                .IsUnique();
        });

        modelBuilder.Entity<ItemAllowedJob>(entity =>
        {
            entity.HasKey(itemAllowedJob => new { itemAllowedJob.ItemId, itemAllowedJob.JobCode });
        });
    }
}
