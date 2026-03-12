using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTestRollbackAndCharacterDeathTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DiedAtUtc",
                table: "Characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameTestAppliedEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameTestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKind = table.Column<int>(type: "integer", nullable: false),
                    TargetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousValue = table.Column<int>(type: "integer", nullable: false),
                    NewValue = table.Column<int>(type: "integer", nullable: false),
                    PreviousIsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    NewIsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    PreviousDiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewDiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTestAppliedEffects", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameTestAppliedEffects");

            migrationBuilder.DropColumn(
                name: "DiedAtUtc",
                table: "Characters");
        }
    }
}
