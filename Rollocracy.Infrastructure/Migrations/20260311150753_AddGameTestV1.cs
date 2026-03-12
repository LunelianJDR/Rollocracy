using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTestV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RolledAt",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "Comparison",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "Modifier",
                table: "GameTests");

            migrationBuilder.RenameColumn(
                name: "TotalResult",
                table: "PlayerTestRolls",
                newName: "FinalValue");

            migrationBuilder.RenameColumn(
                name: "DiceDetails",
                table: "PlayerTestRolls",
                newName: "PlayerNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "TargetValue",
                table: "GameTests",
                newName: "ResolutionModeSnapshot");

            migrationBuilder.RenameColumn(
                name: "TargetType",
                table: "GameTests",
                newName: "DiceSides");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "GameTests",
                newName: "DiceCount");

            migrationBuilder.RenameColumn(
                name: "IsCancelled",
                table: "GameTests",
                newName: "IsClosed");

            migrationBuilder.RenameColumn(
                name: "DiceFormula",
                table: "GameTests",
                newName: "AttributeNameSnapshot");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "GameTests",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "AttributeValueSnapshot",
                table: "PlayerTestRolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CharacterId",
                table: "PlayerTestRolls",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CharacterNameSnapshot",
                table: "PlayerTestRolls",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiceResultsJson",
                table: "PlayerTestRolls",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DiceTotal",
                table: "PlayerTestRolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasRolled",
                table: "PlayerTestRolls",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RolledAtUtc",
                table: "PlayerTestRolls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoRollAtUtc",
                table: "GameTests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "GameTests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessThreshold",
                table: "GameTests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributeValueSnapshot",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "CharacterNameSnapshot",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "DiceResultsJson",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "DiceTotal",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "HasRolled",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "RolledAtUtc",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "AutoRollAtUtc",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "SuccessThreshold",
                table: "GameTests");

            migrationBuilder.RenameColumn(
                name: "PlayerNameSnapshot",
                table: "PlayerTestRolls",
                newName: "DiceDetails");

            migrationBuilder.RenameColumn(
                name: "FinalValue",
                table: "PlayerTestRolls",
                newName: "TotalResult");

            migrationBuilder.RenameColumn(
                name: "ResolutionModeSnapshot",
                table: "GameTests",
                newName: "TargetValue");

            migrationBuilder.RenameColumn(
                name: "IsClosed",
                table: "GameTests",
                newName: "IsCancelled");

            migrationBuilder.RenameColumn(
                name: "DiceSides",
                table: "GameTests",
                newName: "TargetType");

            migrationBuilder.RenameColumn(
                name: "DiceCount",
                table: "GameTests",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "GameTests",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "AttributeNameSnapshot",
                table: "GameTests",
                newName: "DiceFormula");

            migrationBuilder.AddColumn<DateTime>(
                name: "RolledAt",
                table: "PlayerTestRolls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Comparison",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Modifier",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
