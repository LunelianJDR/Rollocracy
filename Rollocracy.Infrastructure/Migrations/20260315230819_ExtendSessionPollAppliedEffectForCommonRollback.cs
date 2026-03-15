using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class ExtendSessionPollAppliedEffectForCommonRollback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "SessionPollAppliedEffects",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "PreviousHasTargetLink",
                table: "SessionPollAppliedEffects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewHasTargetLink",
                table: "SessionPollAppliedEffects",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "SessionPollAppliedEffects");

            migrationBuilder.DropColumn(
                name: "PreviousHasTargetLink",
                table: "SessionPollAppliedEffects");

            migrationBuilder.DropColumn(
                name: "NewHasTargetLink",
                table: "SessionPollAppliedEffects");
        }
    }
}