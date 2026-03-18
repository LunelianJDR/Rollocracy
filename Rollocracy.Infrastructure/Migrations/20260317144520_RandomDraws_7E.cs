using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RandomDraws_7E : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionRandomDraws",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RequestedCount = table.Column<int>(type: "integer", nullable: false),
                    ResultSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRandomDraws", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionRandomDraws_SessionId",
                table: "SessionRandomDraws",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRandomDraws_SessionId_CreatedAtUtc",
                table: "SessionRandomDraws",
                columns: new[] { "SessionId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionRandomDraws");
        }
    }
}
