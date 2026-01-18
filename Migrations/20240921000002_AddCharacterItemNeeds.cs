using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations;

public partial class AddCharacterItemNeeds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CharacterItemNeeds",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                State = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CharacterItemNeeds", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CharacterItemNeeds_CharacterId_ItemId",
            table: "CharacterItemNeeds",
            columns: new[] { "CharacterId", "ItemId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CharacterItemNeeds");
    }
}
