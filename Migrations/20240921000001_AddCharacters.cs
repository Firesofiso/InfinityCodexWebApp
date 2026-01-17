using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations;

public partial class AddCharacters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Characters",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                OwnerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                DataSource = table.Column<string>(type: "TEXT", nullable: false),
                LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Characters", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CharacterItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                ItemId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CharacterItems", x => x.Id);
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

        migrationBuilder.CreateIndex(
            name: "IX_CharacterItems_CharacterId_ItemId",
            table: "CharacterItems",
            columns: new[] { "CharacterId", "ItemId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CharacterJobs_CharacterId_JobCode",
            table: "CharacterJobs",
            columns: new[] { "CharacterId", "JobCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Characters_OwnerUserId_Name",
            table: "Characters",
            columns: new[] { "OwnerUserId", "Name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CharacterItems");

        migrationBuilder.DropTable(
            name: "CharacterJobs");

        migrationBuilder.DropTable(
            name: "Characters");
    }
}
