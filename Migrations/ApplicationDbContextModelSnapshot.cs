using System;
using InfinityCodexWebApp.Data;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace InfinityCodexWebApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("InfinityCodexWebApp.Character", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("DataSource")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<bool>("IsActive")
                .HasColumnType("INTEGER");

            b.Property<DateTime?>("LastSyncedAt")
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<int>("OwnerUserId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("OwnerUserId", "Name")
                .IsUnique();

            b.ToTable("Characters");
        });

        modelBuilder.Entity("InfinityCodexWebApp.CharacterItem", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("CharacterId")
                .HasColumnType("INTEGER");

            b.Property<int>("ItemId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("CharacterId", "ItemId")
                .IsUnique();

            b.ToTable("CharacterItems");
        });

        modelBuilder.Entity("InfinityCodexWebApp.CharacterItemNeed", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("CharacterId")
                .HasColumnType("INTEGER");

            b.Property<int>("ItemId")
                .HasColumnType("INTEGER");

            b.Property<string>("State")
                .IsRequired()
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("CharacterId", "ItemId")
                .IsUnique();

            b.ToTable("CharacterItemNeeds");
        });

        modelBuilder.Entity("InfinityCodexWebApp.CharacterJob", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("CharacterId")
                .HasColumnType("INTEGER");

            b.Property<string>("JobCode")
                .IsRequired()
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("CharacterId", "JobCode")
                .IsUnique();

            b.ToTable("CharacterJobs");
        });

        modelBuilder.Entity("InfinityCodexWebApp.WeatherForecast", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateOnly>("Date")
                .HasColumnType("TEXT");

            b.Property<string>("Summary")
                .HasColumnType("TEXT");

            b.Property<int>("TemperatureC")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.ToTable("WeatherForecasts");
        });
    }
}
