using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class U4_GameMasterAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MonthlyPriceTtc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPlayersPerSession = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlans_MaxPlayersPerSession_Range", "\"MaxPlayersPerSession\" >= 0 AND \"MaxPlayersPerSession\" <= 5000");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRenewalAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelAtRenewal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name" },
                values: new object[,]
                {
                    { new Guid("0c1c29b4-c9b9-4e3b-a4aa-4106d7dd6a10"), "aventurier", 1, true, 15, 0m, "Aventurier" },
                    { new Guid("2f3df112-4d0d-4ef7-8b53-0c6fcb88b701"), "heros", 2, true, 75, 5m, "Héros" },
                    { new Guid("5fa71ab1-3f0c-401d-8c90-6b76a2d2c703"), "legende", 4, true, 500, 20m, "Légende" },
                    { new Guid("8c48c20d-5f5d-4b5a-b997-d863cf7be702"), "champion", 3, true, 200, 10m, "Champion" },
                    { new Guid("8d9d4e4a-9d2b-4204-8c16-2f7ab0cb2f05"), "divin", 6, true, 5000, 100m, "Divin" },
                    { new Guid("c7c1d657-7fd4-42bd-b4f4-7047a6d43704"), "mythique", 5, true, 1500, 50m, "Mythique" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_CurrentPlanId",
                table: "UserSubscriptions",
                column: "CurrentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PendingPlanId",
                table: "UserSubscriptions",
                column: "PendingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserAccountId",
                table: "UserSubscriptions",
                column: "UserAccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");
        }
    }
}
