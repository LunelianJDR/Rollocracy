using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddMaxPlayersPerSessionAndFixPlayerSessionRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPlayersPerSession",
                table: "UserAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccounts_MaxPlayersPerSession_Range",
                table: "UserAccounts",
                sql: "\"MaxPlayersPerSession\" >= 0 AND \"MaxPlayersPerSession\" <= 5000");

            migrationBuilder.Sql(@"
UPDATE ""PlayerSessions"" AS ps
SET ""IsGameMaster"" = CASE
    WHEN s.""GameMasterUserAccountId"" = ps.""UserAccountId"" THEN TRUE
    ELSE FALSE
END
FROM ""Sessions"" AS s
WHERE s.""Id"" = ps.""SessionId"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccounts_MaxPlayersPerSession_Range",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "MaxPlayersPerSession",
                table: "UserAccounts");
        }
    }
}