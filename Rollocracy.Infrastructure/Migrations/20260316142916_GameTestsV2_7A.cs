using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GameTestsV2_7A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AttributeNameSnapshot",
                table: "GameTests",
                newName: "TargetNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "AttributeDefinitionId",
                table: "GameTests",
                newName: "TargetDefinitionId");

            migrationBuilder.AddColumn<int>(
                name: "Outcome",
                table: "PlayerTestRolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CriticalFailureValueSnapshot",
                table: "GameTests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalSuccessValueSnapshot",
                table: "GameTests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetKind",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseSystemDefaultDice",
                table: "GameTests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CriticalFailureValue",
                table: "GameSystems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalSuccessValue",
                table: "GameSystems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTestDiceCount",
                table: "GameSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTestDiceSides",
                table: "GameSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "CriticalFailureValueSnapshot",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "CriticalSuccessValueSnapshot",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "TargetKind",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "UseSystemDefaultDice",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "CriticalFailureValue",
                table: "GameSystems");

            migrationBuilder.DropColumn(
                name: "CriticalSuccessValue",
                table: "GameSystems");

            migrationBuilder.DropColumn(
                name: "DefaultTestDiceCount",
                table: "GameSystems");

            migrationBuilder.DropColumn(
                name: "DefaultTestDiceSides",
                table: "GameSystems");

            migrationBuilder.RenameColumn(
                name: "TargetNameSnapshot",
                table: "GameTests",
                newName: "AttributeNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "TargetDefinitionId",
                table: "GameTests",
                newName: "AttributeDefinitionId");
        }
    }
}
