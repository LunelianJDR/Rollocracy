using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTestDynamicSuccessThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EffectiveSuccessThreshold",
                table: "PlayerTestRolls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuccessThresholdMetricId",
                table: "GameTests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuccessThresholdMetricNameSnapshot",
                table: "GameTests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SuccessThresholdMode",
                table: "GameTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveSuccessThreshold",
                table: "PlayerTestRolls");

            migrationBuilder.DropColumn(
                name: "SuccessThresholdMetricId",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "SuccessThresholdMetricNameSnapshot",
                table: "GameTests");

            migrationBuilder.DropColumn(
                name: "SuccessThresholdMode",
                table: "GameTests");
        }
    }
}
