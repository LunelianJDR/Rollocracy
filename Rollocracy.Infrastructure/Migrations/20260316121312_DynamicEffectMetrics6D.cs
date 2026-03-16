using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DynamicEffectMetrics6D : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceMetricId",
                table: "SessionPollOptionConsequences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "SessionPollOptionConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMetricId",
                table: "GameTestConsequences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "GameTestConsequences",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceMetricId",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "SessionPollOptionConsequences");

            migrationBuilder.DropColumn(
                name: "SourceMetricId",
                table: "GameTestConsequences");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "GameTestConsequences");
        }
    }
}
