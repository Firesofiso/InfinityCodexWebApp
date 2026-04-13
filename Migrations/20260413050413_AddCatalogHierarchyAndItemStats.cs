using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogHierarchyAndItemStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipSlotGroup",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExclusive",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRare",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawEffectText",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentGroupId",
                table: "ContentSources",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ContentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentGroups", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ContentGroups",
                columns: new[] { "Id", "Name", "Tag", "Notes", "IsActive" },
                values: new object[] { 1, "Legacy", "legacy", "Default group for existing content sources.", true });

            migrationBuilder.InsertData(
                table: "ContentGroups",
                columns: new[] { "Id", "Name", "Tag", "Notes", "IsActive" },
                values: new object[] { 2, "Dynamis", "dynamis", "Auto-assigned for Dynamis sub-sources.", true });

            migrationBuilder.CreateTable(
                name: "ItemAccessoryStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Charges = table.Column<int>(type: "INTEGER", nullable: true),
                    RecastSeconds = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAccessoryStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ItemAccessoryStats_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemArmorStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: true),
                    MagicDefense = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemArmorStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ItemArmorStats_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemStatModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatKey = table.Column<string>(type: "TEXT", nullable: false),
                    StatValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStatModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStatModifiers_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemWeaponStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: true),
                    Delay = table.Column<int>(type: "INTEGER", nullable: true),
                    Dps = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemWeaponStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ItemWeaponStats_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_ContentGroupId_Tag",
                table: "ContentSources",
                columns: new[] { "ContentGroupId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentGroups_Tag",
                table: "ContentGroups",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemStatModifiers_ItemId_SortOrder",
                table: "ItemStatModifiers",
                columns: new[] { "ItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemStatModifiers_ItemId_StatKey",
                table: "ItemStatModifiers",
                columns: new[] { "ItemId", "StatKey" });

            migrationBuilder.Sql(
                "UPDATE ContentSources SET ContentGroupId = 2 WHERE Name LIKE 'Dynamis - %' OR Tag LIKE 'dynamis-%';");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentSources_ContentGroups_ContentGroupId",
                table: "ContentSources",
                column: "ContentGroupId",
                principalTable: "ContentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentSources_ContentGroups_ContentGroupId",
                table: "ContentSources");

            migrationBuilder.DropTable(
                name: "ContentGroups");

            migrationBuilder.DropTable(
                name: "ItemAccessoryStats");

            migrationBuilder.DropTable(
                name: "ItemArmorStats");

            migrationBuilder.DropTable(
                name: "ItemStatModifiers");

            migrationBuilder.DropTable(
                name: "ItemWeaponStats");

            migrationBuilder.DropIndex(
                name: "IX_ContentSources_ContentGroupId_Tag",
                table: "ContentSources");

            migrationBuilder.DropColumn(
                name: "EquipSlotGroup",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsExclusive",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsRare",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RawEffectText",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ContentGroupId",
                table: "ContentSources");
        }
    }
}
