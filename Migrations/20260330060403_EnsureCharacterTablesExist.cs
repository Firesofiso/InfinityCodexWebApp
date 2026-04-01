using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class EnsureCharacterTablesExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Characters" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Characters" PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "OwnerUserId" INTEGER NOT NULL,
                    "IsActive" INTEGER NOT NULL,
                    "DataSource" TEXT NOT NULL,
                    "LastSyncedAt" TEXT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CharacterItems" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CharacterItems" PRIMARY KEY AUTOINCREMENT,
                    "CharacterId" INTEGER NOT NULL,
                    "ItemId" INTEGER NOT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CharacterJobs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CharacterJobs" PRIMARY KEY AUTOINCREMENT,
                    "CharacterId" INTEGER NOT NULL,
                    "JobCode" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "CharacterItemNeeds" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CharacterItemNeeds" PRIMARY KEY AUTOINCREMENT,
                    "CharacterId" INTEGER NOT NULL,
                    "ItemId" INTEGER NOT NULL,
                    "State" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Characters_OwnerUserId_Name\" ON \"Characters\" (\"OwnerUserId\", \"Name\");");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CharacterItems_CharacterId_ItemId\" ON \"CharacterItems\" (\"CharacterId\", \"ItemId\");");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CharacterJobs_CharacterId_JobCode\" ON \"CharacterJobs\" (\"CharacterId\", \"JobCode\");");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CharacterItemNeeds_CharacterId_ItemId\" ON \"CharacterItemNeeds\" (\"CharacterId\", \"ItemId\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CharacterItemNeeds_CharacterId_ItemId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CharacterJobs_CharacterId_JobCode\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CharacterItems_CharacterId_ItemId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Characters_OwnerUserId_Name\";");

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"CharacterItemNeeds\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"CharacterJobs\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"CharacterItems\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Characters\";");
        }
    }
}
