using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraitCreationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLockedForCharacterCreation",
                table: "TraitOptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRandomSelectionGroup",
                table: "TraitDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLockedForCharacterCreation",
                table: "TraitOptions");

            migrationBuilder.DropColumn(
                name: "IsRandomSelectionGroup",
                table: "TraitDefinitions");
        }
    }
}
