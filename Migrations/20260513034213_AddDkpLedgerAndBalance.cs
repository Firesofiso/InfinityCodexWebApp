using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfinityCodexWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDkpLedgerAndBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DkpBalance",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DkpEarnEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpEarnEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DkpEarnEvents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DkpTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: true),
                    EarnEventId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    BalanceAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkpTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DkpTransactions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DkpTransactions_DkpEarnEvents_EarnEventId",
                        column: x => x.EarnEventId,
                        principalTable: "DkpEarnEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DkpTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DkpTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DkpEarnEvents_CreatedByUserId",
                table: "DkpEarnEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpEarnEvents_OccurredAt",
                table: "DkpEarnEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_DkpTransactions_CharacterId",
                table: "DkpTransactions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpTransactions_CreatedByUserId",
                table: "DkpTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DkpTransactions_EarnEventId_CreatedAt",
                table: "DkpTransactions",
                columns: new[] { "EarnEventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DkpTransactions_UserId_CreatedAt",
                table: "DkpTransactions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DkpTransactions");

            migrationBuilder.DropTable(
                name: "DkpEarnEvents");

            migrationBuilder.DropColumn(
                name: "DkpBalance",
                table: "Users");
        }
    }
}
