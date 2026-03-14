using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddMetricsAndAutomaticPollVoteWeight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseValue",
                table: "MetricDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinValue",
                table: "MetricDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxValue",
                table: "MetricDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "VoteWeightMode",
                table: "SessionPolls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MetricDefinitionId",
                table: "SessionPolls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetricNameSnapshot",
                table: "SessionPolls",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPolls_MetricDefinitionId",
                table: "SessionPolls",
                column: "MetricDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionPolls_MetricDefinitions_MetricDefinitionId",
                table: "SessionPolls",
                column: "MetricDefinitionId",
                principalTable: "MetricDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionPolls_MetricDefinitions_MetricDefinitionId",
                table: "SessionPolls");

            migrationBuilder.DropIndex(
                name: "IX_SessionPolls_MetricDefinitionId",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "BaseValue",
                table: "MetricDefinitions");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "MetricDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "MetricDefinitions");

            migrationBuilder.DropColumn(
                name: "VoteWeightMode",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "MetricDefinitionId",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "MetricNameSnapshot",
                table: "SessionPolls");
        }
    }
}