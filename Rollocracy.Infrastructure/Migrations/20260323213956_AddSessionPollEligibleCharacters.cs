using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPollEligibleCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionPollEligibleCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPollEligibleCharacters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionPollEligibleCharacters_CharacterId",
                table: "SessionPollEligibleCharacters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPollEligibleCharacters_SessionPollId",
                table: "SessionPollEligibleCharacters",
                column: "SessionPollId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPollEligibleCharacters_SessionPollId_CharacterId",
                table: "SessionPollEligibleCharacters",
                columns: new[] { "SessionPollId", "CharacterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionPollEligibleCharacters");
        }
    }
}
