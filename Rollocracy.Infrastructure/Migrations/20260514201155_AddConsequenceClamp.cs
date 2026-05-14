using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsequenceClamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClampMaxTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "ClampMinTotal",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: -100);

            migrationBuilder.AddColumn<int>(
                name: "ClampMode",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClampMaxTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "ClampMinTotal",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: -100);

            migrationBuilder.AddColumn<int>(
                name: "ClampMode",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClampMaxTotal",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "ClampMinTotal",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "ClampMode",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "ClampMaxTotal",
                table: "GameTestConsequences");

            migrationBuilder.DropColumn(
                name: "ClampMinTotal",
                table: "GameTestConsequences");

            migrationBuilder.DropColumn(
                name: "ClampMode",
                table: "GameTestConsequences");
        }
    }
}
