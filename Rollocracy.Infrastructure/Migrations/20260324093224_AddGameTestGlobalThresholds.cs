using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTestGlobalThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GlobalConsequencesApplied",
                table: "GameTests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GlobalSuccessThreshold1Percent",
                table: "GameTests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GlobalSuccessThreshold2Percent",
                table: "GameTests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GlobalSuccessThreshold3Percent",
                table: "GameTests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalConsequencesApplied",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "GlobalSuccessThreshold1Percent",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "GlobalSuccessThreshold2Percent",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "GlobalSuccessThreshold3Percent",
                table: "GameTests");
        }
    }
}
