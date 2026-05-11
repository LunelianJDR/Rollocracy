using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterItemActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CharacterItems");
        }
    }
}
