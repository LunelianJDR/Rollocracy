using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixClampZeroBoundsDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
