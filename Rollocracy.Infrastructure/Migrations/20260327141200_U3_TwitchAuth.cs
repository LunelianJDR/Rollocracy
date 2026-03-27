using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class U3_TwitchAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwitchDisplayName",
                table: "UserAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitchUserId",
                table: "UserAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TwitchPendingAuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicToken = table.Column<string>(type: "text", nullable: false),
                    OAuthState = table.Column<string>(type: "text", nullable: false),
                    FlowType = table.Column<string>(type: "text", nullable: false),
                    CurrentUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    TwitchUserId = table.Column<string>(type: "text", nullable: true),
                    TwitchLogin = table.Column<string>(type: "text", nullable: true),
                    TwitchDisplayName = table.Column<string>(type: "text", nullable: true),
                    TwitchEmail = table.Column<string>(type: "text", nullable: true),
                    MatchedUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchPendingAuthSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TwitchPendingAuthSessions_CurrentUserAccountId",
                table: "TwitchPendingAuthSessions",
                column: "CurrentUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TwitchPendingAuthSessions_ExpiresAtUtc",
                table: "TwitchPendingAuthSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TwitchPendingAuthSessions_MatchedUserAccountId",
                table: "TwitchPendingAuthSessions",
                column: "MatchedUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TwitchPendingAuthSessions_OAuthState",
                table: "TwitchPendingAuthSessions",
                column: "OAuthState",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchPendingAuthSessions_PublicToken",
                table: "TwitchPendingAuthSessions",
                column: "PublicToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchPendingAuthSessions");

            migrationBuilder.DropColumn(
                name: "TwitchDisplayName",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "TwitchUserId",
                table: "UserAccounts");
        }
    }
}
