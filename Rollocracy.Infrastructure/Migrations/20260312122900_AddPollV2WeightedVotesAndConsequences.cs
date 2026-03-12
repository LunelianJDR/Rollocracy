using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPollV2WeightedVotesAndConsequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VoteWeight",
                table: "SessionPollVotes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ConsequencesApplied",
                table: "SessionPolls",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SessionPollAppliedEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollVoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollOptionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SessionPollAppliedEffects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionPollOptionConsequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKind = table.Column<int>(type: "integer", nullable: false),
                    TargetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    ModifierMode = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPollOptionConsequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionPollWeightRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionPollId = table.Column<Guid>(type: "uuid", nullable: false),
                    TraitDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TraitOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightBonus = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPollWeightRules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionPollAppliedEffects");

            migrationBuilder.DropTable(
                name: "SessionPollOptionConsequences");

            migrationBuilder.DropTable(
                name: "SessionPollWeightRules");

            migrationBuilder.DropColumn(
                name: "VoteWeight",
                table: "SessionPollVotes");

            migrationBuilder.DropColumn(
                name: "ConsequencesApplied",
                table: "SessionPolls");
        }
    }
}
