using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateCharacterModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterJobLevels");

            migrationBuilder.DropTable(
                name: "CharacterJobs");

            migrationBuilder.DropTable(
                name: "CharacterMissionProgresses");

            migrationBuilder.DropColumn(
                name: "PreferredJobsCsv",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "BastokMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChainsOfPromathiaMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftAlchemyLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftBonecraftLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftClothcraftLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftCookingLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftFishingLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftGoldsmithingLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftLeathercraftLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftSmithingLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CraftWoodworkingLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisBastok",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisBeaucedine",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisBuburimu",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisJeuno",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisQufim",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisSandOria",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisTavnazia",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisValkurm",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisWindurst",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DynamisXarcabard",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EpilogueMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobBlmLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobBrdLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobBstLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobDrgLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobDrkLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobMnkLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobNinLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobPldLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobRdmLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobRngLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobSamLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobSmnLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobThfLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobWarLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobWhmLevel",
                table: "Characters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiseOfTheZilartMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SandOriaMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WindurstMission",
                table: "Characters",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BastokMission",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ChainsOfPromathiaMission",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftAlchemyLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftBonecraftLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftClothcraftLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftCookingLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftFishingLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftGoldsmithingLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftLeathercraftLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftSmithingLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CraftWoodworkingLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisBastok",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisBeaucedine",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisBuburimu",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisJeuno",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisQufim",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisSandOria",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisTavnazia",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisValkurm",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisWindurst",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DynamisXarcabard",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "EpilogueMission",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobBlmLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobBrdLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobBstLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobDrgLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobDrkLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobMnkLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobNinLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobPldLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobRdmLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobRngLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobSamLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobSmnLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobThfLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobWarLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "JobWhmLevel",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RiseOfTheZilartMission",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SandOriaMission",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "WindurstMission",
                table: "Characters");

            migrationBuilder.AddColumn<string>(
                name: "PreferredJobsCsv",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CharacterJobLevels",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobCode = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterJobLevels", x => new { x.CharacterId, x.JobCode });
                });

            migrationBuilder.CreateTable(
                name: "CharacterJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobCode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterMissionProgresses",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    BastokMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ChainsOfPromathiaMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EpilogueMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RiseOfTheZilartMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SanDOriaMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindurstMission = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMissionProgresses", x => x.CharacterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterJobs_CharacterId_JobCode",
                table: "CharacterJobs",
                columns: new[] { "CharacterId", "JobCode" },
                unique: true);
        }
    }
}
