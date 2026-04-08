using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M3_CharacterCreationEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelectableAtCharacterCreation",
                table: "TalentDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelectableAtCharacterCreation",
                table: "ItemDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartingItemChoices",
                table: "GameSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartingTalentChoices",
                table: "GameSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreationDistributionPoints",
                table: "DerivedStatDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCreationDistributionPerCharacter",
                table: "DerivedStatDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreationDistributionPoints",
                table: "AttributeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCreationDistributionPerCharacter",
                table: "AttributeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelectableAtCharacterCreation",
                table: "TalentDefinitions");

            migrationBuilder.DropColumn(
                name: "IsSelectableAtCharacterCreation",
                table: "ItemDefinitions");

            migrationBuilder.DropColumn(
                name: "StartingItemChoices",
                table: "GameSystems");

            migrationBuilder.DropColumn(
                name: "StartingTalentChoices",
                table: "GameSystems");

            migrationBuilder.DropColumn(
                name: "CreationDistributionPoints",
                table: "DerivedStatDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxCreationDistributionPerCharacter",
                table: "DerivedStatDefinitions");

            migrationBuilder.DropColumn(
                name: "CreationDistributionPoints",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxCreationDistributionPerCharacter",
                table: "AttributeDefinitions");
        }
    }
}
