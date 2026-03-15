using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddCharacterModifiersFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddValue = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterModifiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterModifiers_CharacterId",
                table: "CharacterModifiers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterModifiers_CharacterId_TargetType_TargetId",
                table: "CharacterModifiers",
                columns: new[] { "CharacterId", "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterModifiers_SourceType_SourceId",
                table: "CharacterModifiers",
                columns: new[] { "SourceType", "SourceId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterModifiers");
        }
    }
}
