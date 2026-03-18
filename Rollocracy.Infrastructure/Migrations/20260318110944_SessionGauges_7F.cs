using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionGauges_7F : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionGauges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinValue = table.Column<int>(type: "integer", nullable: false),
                    MaxValue = table.Column<int>(type: "integer", nullable: false),
                    CurrentValue = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionGauges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionGauges_SessionId",
                table: "SessionGauges",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionGauges_SessionId_Name",
                table: "SessionGauges",
                columns: new[] { "SessionId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionGauges");
        }
    }
}
