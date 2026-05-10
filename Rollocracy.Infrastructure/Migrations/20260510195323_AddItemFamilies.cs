using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemFamilyDefinitionId",
                table: "ItemDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemFamilyDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MaxOwned = table.Column<int>(type: "integer", nullable: false),
                    MaxActive = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemFamilyDefinitions", x => x.Id);
                    table.CheckConstraint(
                        "CK_ItemFamilyDefinitions_MaxActive_Range",
                        "\"MaxActive\" >= 0 AND \"MaxActive\" <= \"MaxOwned\"");
                    table.CheckConstraint(
                        "CK_ItemFamilyDefinitions_MaxOwned_Range",
                        "\"MaxOwned\" >= 1 AND \"MaxOwned\" <= 99");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefinitions_ItemFamilyDefinitionId",
                table: "ItemDefinitions",
                column: "ItemFamilyDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemFamilyDefinitions_GameSystemId",
                table: "ItemFamilyDefinitions",
                column: "GameSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemFamilyDefinitions_GameSystemId_DisplayOrder",
                table: "ItemFamilyDefinitions",
                columns: new[] { "GameSystemId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ItemDefinitions_ItemFamilyDefinitions_ItemFamilyDefinitionId",
                table: "ItemDefinitions",
                column: "ItemFamilyDefinitionId",
                principalTable: "ItemFamilyDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemDefinitions_ItemFamilyDefinitions_ItemFamilyDefinitionId",
                table: "ItemDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ItemDefinitions_ItemFamilyDefinitionId",
                table: "ItemDefinitions");

            migrationBuilder.DropTable(
                name: "ItemFamilyDefinitions");

            migrationBuilder.DropColumn(
                name: "ItemFamilyDefinitionId",
                table: "ItemDefinitions");
        }
    }
}