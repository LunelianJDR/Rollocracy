using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTestV2DifficultyFiltersConsequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "GameTestConsequences",
                newName: "TargetKind");

            migrationBuilder.RenameColumn(
                name: "PreviousValue",
                table: "GameTestConsequences",
                newName: "ModifierMode");

            migrationBuilder.RenameColumn(
                name: "CharacterId",
                table: "GameTestConsequences",
                newName: "TargetDefinitionId");

            migrationBuilder.AddColumn<int>(
                name: "EffectiveAttributeValue",
                table: "PlayerTestRolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DifficultyValue",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifierMode",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetScope",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApplyOn",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetNameSnapshot",
                table: "GameTestConsequences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GameTestTraitFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TraitDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TraitOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTestTraitFilters", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameTestTraitFilters");

            migrationBuilder.DropColumn(
                name: "EffectiveAttributeValue",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "DifficultyValue",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "ModifierMode",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "TargetScope",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "ApplyOn",
                table: "GameTestConsequences");

            migrationBuilder.DropColumn(
                name: "TargetNameSnapshot",
                table: "GameTestConsequences");

            migrationBuilder.RenameColumn(
                name: "TargetKind",
                table: "GameTestConsequences",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "TargetDefinitionId",
                table: "GameTestConsequences",
                newName: "CharacterId");

            migrationBuilder.RenameColumn(
                name: "ModifierMode",
                table: "GameTestConsequences",
                newName: "PreviousValue");
        }
    }
}
