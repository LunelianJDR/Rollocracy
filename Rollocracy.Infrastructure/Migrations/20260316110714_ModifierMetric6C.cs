using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifierMetric6C : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceMetricId",
                table: "TalentModifierDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "TalentModifierDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMetricId",
                table: "ItemModifierDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "ItemModifierDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMetricId",
                table: "ChoiceOptionModifierDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueMode",
                table: "ChoiceOptionModifierDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceMetricId",
                table: "TalentModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "TalentModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "SourceMetricId",
                table: "ItemModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "ItemModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "SourceMetricId",
                table: "ChoiceOptionModifierDefinitions");

            migrationBuilder.DropColumn(
                name: "ValueMode",
                table: "ChoiceOptionModifierDefinitions");
        }
    }
}
