using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Streamers");

            migrationBuilder.RenameColumn(
                name: "StreamerId",
                table: "Sessions",
                newName: "GameMasterUserAccountId");

            migrationBuilder.RenameColumn(
                name: "SessionCode",
                table: "Sessions",
                newName: "SessionSlug");

            migrationBuilder.RenameColumn(
                name: "StreamerId",
                table: "GameSystems",
                newName: "OwnerUserAccountId");

            migrationBuilder.AddColumn<string>(
                name: "SessionPasswordHash",
                table: "Sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UserAccountId",
                table: "PlayerSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsGameMaster = table.Column<bool>(type: "boolean", nullable: false),
                    IsTwitchLinked = table.Column<bool>(type: "boolean", nullable: false),
                    TwitchLogin = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_GameMasterUserAccountId_SessionSlug",
                table: "Sessions",
                columns: new[] { "GameMasterUserAccountId", "SessionSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_GameMasterUserAccountId_SessionSlug",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SessionPasswordHash",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "UserAccountId",
                table: "PlayerSessions");

            migrationBuilder.RenameColumn(
                name: "SessionSlug",
                table: "Sessions",
                newName: "SessionCode");

            migrationBuilder.RenameColumn(
                name: "GameMasterUserAccountId",
                table: "Sessions",
                newName: "StreamerId");

            migrationBuilder.RenameColumn(
                name: "OwnerUserAccountId",
                table: "GameSystems",
                newName: "StreamerId");

            migrationBuilder.CreateTable(
                name: "Streamers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    TwitchId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streamers", x => x.Id);
                });
        }
    }
}
