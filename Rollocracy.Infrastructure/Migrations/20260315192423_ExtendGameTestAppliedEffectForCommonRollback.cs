using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class ExtendGameTestAppliedEffectForCommonRollback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "GameTestAppliedEffects",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "PreviousHasTargetLink",
                table: "GameTestAppliedEffects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewHasTargetLink",
                table: "GameTestAppliedEffects",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "GameTestAppliedEffects");

            migrationBuilder.DropColumn(
                name: "PreviousHasTargetLink",
                table: "GameTestAppliedEffects");

            migrationBuilder.DropColumn(
                name: "NewHasTargetLink",
                table: "GameTestAppliedEffects");
        }
    }
}