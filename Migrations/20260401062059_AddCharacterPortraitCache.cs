using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterPortraitCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortraitUrl",
                table: "Characters",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortraitUrl",
                table: "Characters");
        }
    }
}
