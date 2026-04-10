using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M4_ConsumableItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FillGaugeCurrentValueOnly",
                table: "ItemModifierDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "ItemModifierDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsConsumable",
                table: "ItemDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantityPerCharacter",
                table: "ItemDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FillGaugeCurrentValueOnly",
                table: "ItemModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "ItemModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "IsConsumable",
                table: "ItemDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxQuantityPerCharacter",
                table: "ItemDefinitions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "CharacterItems");
        }
    }
}
