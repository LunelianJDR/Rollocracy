using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddMassDistributionUndoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUndone",
                table: "MassDistributionBatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime?>(
                name: "UndoneAtUtc",
                table: "MassDistributionBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UndoSnapshotJson",
                table: "MassDistributionBatches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUndone",
                table: "MassDistributionBatches");

            migrationBuilder.DropColumn(
                name: "UndoneAtUtc",
                table: "MassDistributionBatches");

            migrationBuilder.DropColumn(
                name: "UndoSnapshotJson",
                table: "MassDistributionBatches");
        }
    }
}
