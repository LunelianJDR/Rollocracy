using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M5A1_SessionPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionGameTestPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionGameTestPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionPollPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPollPresets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionGameTestPresets_SessionId",
                table: "SessionGameTestPresets",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionGameTestPresets_SessionId_Name",
                table: "SessionGameTestPresets",
                columns: new[] { "SessionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionPollPresets_SessionId",
                table: "SessionPollPresets",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPollPresets_SessionId_Name",
                table: "SessionPollPresets",
                columns: new[] { "SessionId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionGameTestPresets");

            migrationBuilder.DropTable(
                name: "SessionPollPresets");
        }
    }
}
