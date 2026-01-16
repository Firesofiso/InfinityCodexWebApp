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
