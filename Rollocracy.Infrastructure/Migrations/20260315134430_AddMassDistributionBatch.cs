using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddMassDistributionBatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MassDistributionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TargetCharacterCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FilterSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    EffectsSnapshotJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassDistributionBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MassDistributionBatches_SessionId",
                table: "MassDistributionBatches",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MassDistributionBatches_SessionId_CreatedAtUtc",
                table: "MassDistributionBatches",
                columns: new[] { "SessionId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MassDistributionBatches");
        }
    }
}
