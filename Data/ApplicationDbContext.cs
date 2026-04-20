using Microsoft.EntityFrameworkCore;

namespace InfinityCodexWebApp.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferredJob> UserPreferredJobs => Set<UserPreferredJob>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterItem> CharacterItems => Set<CharacterItem>();
    public DbSet<CharacterItemNeed> CharacterItemNeeds => Set<CharacterItemNeed>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemArmorStats> ItemArmorStats => Set<ItemArmorStats>();
    public DbSet<ItemWeaponStats> ItemWeaponStats => Set<ItemWeaponStats>();
    public DbSet<ItemAccessoryStats> ItemAccessoryStats => Set<ItemAccessoryStats>();
    public DbSet<ItemStatModifier> ItemStatModifiers => Set<ItemStatModifier>();
    public DbSet<ItemAllowedJob> ItemAllowedJobs => Set<ItemAllowedJob>();
    public DbSet<ItemSource> ItemSources => Set<ItemSource>();
    public DbSet<ContentGroup> ContentGroups => Set<ContentGroup>();
    public DbSet<ContentSource> ContentSources => Set<ContentSource>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.DiscordId)
                .IsUnique();
        });

        modelBuilder.Entity<UserPreferredJob>(entity =>
        {
            entity.HasIndex(userPreferredJob => new { userPreferredJob.UserId, userPreferredJob.JobCode })
                .IsUnique();
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasIndex(character => new { character.OwnerUserId, character.Name })
                .IsUnique();
        });

        modelBuilder.Entity<CharacterItem>(entity =>
        {
            entity.HasIndex(characterItem => new { characterItem.CharacterId, characterItem.ItemId })
                .IsUnique();
        });

        modelBuilder.Entity<CharacterItemNeed>(entity =>
        {
            entity.HasIndex(characterItemNeed => new { characterItemNeed.CharacterId, characterItemNeed.ItemId })
                .IsUnique();
        });

        modelBuilder.Entity<ItemAllowedJob>(entity =>
        {
            entity.HasKey(itemAllowedJob => new { itemAllowedJob.ItemId, itemAllowedJob.JobCode });
        });

        modelBuilder.Entity<ItemArmorStats>(entity =>
        {
            entity.HasKey(itemArmorStats => itemArmorStats.ItemId);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(itemArmorStats => itemArmorStats.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemWeaponStats>(entity =>
        {
            entity.HasKey(itemWeaponStats => itemWeaponStats.ItemId);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(itemWeaponStats => itemWeaponStats.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemAccessoryStats>(entity =>
        {
            entity.HasKey(itemAccessoryStats => itemAccessoryStats.ItemId);
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(itemAccessoryStats => itemAccessoryStats.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemStatModifier>(entity =>
        {
            entity.HasIndex(itemStatModifier => new { itemStatModifier.ItemId, itemStatModifier.SortOrder });
            entity.HasIndex(itemStatModifier => new { itemStatModifier.ItemId, itemStatModifier.StatKey });
            entity.HasOne<Item>()
                .WithMany()
                .HasForeignKey(itemStatModifier => itemStatModifier.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemSource>(entity =>
        {
            entity.HasKey(itemSource => new { itemSource.ItemId, itemSource.ContentSourceId });
        });

        modelBuilder.Entity<ContentGroup>(entity =>
        {
            entity.HasIndex(contentGroup => contentGroup.Tag)
                .IsUnique();
        });

        modelBuilder.Entity<ContentSource>(entity =>
        {
            entity.HasIndex(contentSource => new { contentSource.ContentGroupId, contentSource.Tag });
            entity.HasOne<ContentGroup>()
                .WithMany()
                .HasForeignKey(contentSource => contentSource.ContentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
