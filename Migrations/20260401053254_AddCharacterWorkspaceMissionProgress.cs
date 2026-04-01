using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterWorkspaceMissionProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterMissionProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    SanDOriaMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BastokMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    WindurstMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RiseOfTheZilartMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ChainsOfPromathiaMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMissionProgresses", x => x.CharacterId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterMissionProgresses");
        }
    }
}
