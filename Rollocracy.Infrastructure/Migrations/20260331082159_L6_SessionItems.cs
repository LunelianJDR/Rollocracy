using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class L6_SessionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "GameSystemId",
                table: "ItemDefinitions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "ItemDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefinitions_GameSystemId",
                table: "ItemDefinitions",
                column: "GameSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefinitions_GameSystemId_DisplayOrder",
                table: "ItemDefinitions",
                columns: new[] { "GameSystemId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefinitions_SessionId",
                table: "ItemDefinitions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefinitions_SessionId_DisplayOrder",
                table: "ItemDefinitions",
                columns: new[] { "SessionId", "DisplayOrder" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ItemDefinitions_Scope",
                table: "ItemDefinitions",
                sql: "(\"GameSystemId\" IS NOT NULL AND \"SessionId\" IS NULL) OR (\"GameSystemId\" IS NULL AND \"SessionId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItems_CharacterId",
                table: "CharacterItems",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItems_CharacterId_ItemDefinitionId",
                table: "CharacterItems",
                columns: new[] { "CharacterId", "ItemDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterItems_ItemDefinitionId",
                table: "CharacterItems",
                column: "ItemDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemDefinitions_GameSystemId",
                table: "ItemDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ItemDefinitions_GameSystemId_DisplayOrder",
                table: "ItemDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ItemDefinitions_SessionId",
                table: "ItemDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ItemDefinitions_SessionId_DisplayOrder",
                table: "ItemDefinitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ItemDefinitions_Scope",
                table: "ItemDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_CharacterItems_CharacterId",
                table: "CharacterItems");

            migrationBuilder.DropIndex(
                name: "IX_CharacterItems_CharacterId_ItemDefinitionId",
                table: "CharacterItems");

            migrationBuilder.DropIndex(
                name: "IX_CharacterItems_ItemDefinitionId",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ItemDefinitions");

            migrationBuilder.AlterColumn<Guid>(
                name: "GameSystemId",
                table: "ItemDefinitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
