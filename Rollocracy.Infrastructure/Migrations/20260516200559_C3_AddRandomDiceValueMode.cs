using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class C3_AddRandomDiceValueMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClampMinTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -100);

            migrationBuilder.AlterColumn<int>(
                name: "ClampMaxTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "RandomDiceCount",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RandomDiceSides",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "ClampMinTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -100);

            migrationBuilder.AlterColumn<int>(
                name: "ClampMaxTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "RandomDiceCount",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RandomDiceSides",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomDiceCount",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "RandomDiceSides",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "RandomDiceCount",
                table: "GameTestConsequences");

            migrationBuilder.DropColumn(
                name: "RandomDiceSides",
                table: "GameTestConsequences");

            migrationBuilder.AlterColumn<int>(
                name: "ClampMinTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: -100,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ClampMaxTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ClampMinTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: -100,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ClampMaxTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
