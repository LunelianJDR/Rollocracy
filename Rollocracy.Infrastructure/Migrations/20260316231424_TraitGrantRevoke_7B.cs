using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TraitGrantRevoke_7B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TraitOptionId",
                table: "ChoiceOptionModifierDefinitions",
                newName: "ChoiceOptionDefinitionId");

            migrationBuilder.RenameColumn(
                name: "AddValue",
                table: "ChoiceOptionModifierDefinitions",
                newName: "Value");

            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "ChoiceOptionModifierDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetNameSnapshot",
                table: "ChoiceOptionModifierDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "ChoiceOptionModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "TargetNameSnapshot",
                table: "ChoiceOptionModifierDefinitions");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "ChoiceOptionModifierDefinitions",
                newName: "AddValue");

            migrationBuilder.RenameColumn(
                name: "ChoiceOptionDefinitionId",
                table: "ChoiceOptionModifierDefinitions",
                newName: "TraitOptionId");
        }
    }
}
