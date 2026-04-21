using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M6_SessionStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionStoreOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionStoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferType = table.Column<int>(type: "integer", nullable: false),
                    TargetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencySessionGaugeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionStoreOffers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionStores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionStoreOffers_SessionStoreId",
                table: "SessionStoreOffers",
                column: "SessionStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionStoreOffers_SessionStoreId_DisplayOrder",
                table: "SessionStoreOffers",
                columns: new[] { "SessionStoreId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionStores_SessionId",
                table: "SessionStores",
                column: "SessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionStoreOffers");

            migrationBuilder.DropTable(
                name: "SessionStores");
        }
    }
}
