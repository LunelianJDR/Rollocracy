using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatchNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlannedFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TextFr = table.Column<string>(type: "text", nullable: false),
                    TextEn = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatchNoteEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatchNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextFr = table.Column<string>(type: "text", nullable: false),
                    TextEn = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchNoteEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatchNoteEntries_PatchNotes_PatchNoteId",
                        column: x => x.PatchNoteId,
                        principalTable: "PatchNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatchNoteEntries_PatchNoteId",
                table: "PatchNoteEntries",
                column: "PatchNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchNoteEntries_PatchNoteId_DisplayOrder",
                table: "PatchNoteEntries",
                columns: new[] { "PatchNoteId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PatchNotes_IsPublished_DisplayOrder_PublishedAtUtc",
                table: "PatchNotes",
                columns: new[] { "IsPublished", "DisplayOrder", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PatchNotes_Version",
                table: "PatchNotes",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedFeatures_IsActive_DisplayOrder",
                table: "PlannedFeatures",
                columns: new[] { "IsActive", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatchNoteEntries");

            migrationBuilder.DropTable(
                name: "PlannedFeatures");

            migrationBuilder.DropTable(
                name: "PatchNotes");
        }
    }
}
