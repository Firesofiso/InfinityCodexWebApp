using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEpilogueMissionToCharacterProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EpilogueMission",
                table: "CharacterMissionProgresses",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpilogueMission",
                table: "CharacterMissionProgresses");
        }
    }
}
