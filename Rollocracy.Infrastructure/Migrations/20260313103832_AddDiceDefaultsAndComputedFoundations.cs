using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddDiceDefaultsAndComputedFoundations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ------------------------------------------------------------------
            // Extension de AttributeDefinition pour supporter les valeurs par dés
            // ------------------------------------------------------------------

            migrationBuilder.AddColumn<int>(
                name: "DefaultValueMode",
                table: "AttributeDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultValueFlatBonus",
                table: "AttributeDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultValueDiceCount",
                table: "AttributeDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultValueDiceSides",
                table: "AttributeDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ------------------------------------------------------------------
            // Tables pour les statistiques dérivées
            // ------------------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "DerivedStatDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GameSystemId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 120, nullable: false),
                    RoundMode = table.Column<int>(nullable: false),
                    DisplayOrder = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedStatDefinitions", x => x.Id);

                    table.ForeignKey(
                        name: "FK_DerivedStatDefinitions_GameSystems_GameSystemId",
                        column: x => x.GameSystemId,
                        principalTable: "GameSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DerivedStatComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DerivedStatDefinitionId = table.Column<Guid>(nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(nullable: false),
                    Weight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedStatComponents", x => x.Id);

                    table.ForeignKey(
                        name: "FK_DerivedStatComponents_DerivedStatDefinitions_DerivedStatDefinitionId",
                        column: x => x.DerivedStatDefinitionId,
                        principalTable: "DerivedStatDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_DerivedStatComponents_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ------------------------------------------------------------------
            // Tables pour les metrics (ex : poids de vote futur)
            // ------------------------------------------------------------------

            migrationBuilder.CreateTable(
                name: "MetricDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GameSystemId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 120, nullable: false),
                    RoundMode = table.Column<int>(nullable: false),
                    DisplayOrder = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricDefinitions", x => x.Id);

                    table.ForeignKey(
                        name: "FK_MetricDefinitions_GameSystems_GameSystemId",
                        column: x => x.GameSystemId,
                        principalTable: "GameSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MetricDefinitionId = table.Column<Guid>(nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(nullable: false),
                    Weight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricComponents", x => x.Id);

                    table.ForeignKey(
                        name: "FK_MetricComponents_MetricDefinitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "MetricDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_MetricComponents_AttributeDefinitions_AttributeDefinitionId",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ------------------------------------------------------------------
            // Index utiles
            // ------------------------------------------------------------------

            migrationBuilder.CreateIndex(
                name: "IX_DerivedStatDefinitions_GameSystemId",
                table: "DerivedStatDefinitions",
                column: "GameSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedStatComponents_DerivedStatDefinitionId",
                table: "DerivedStatComponents",
                column: "DerivedStatDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedStatComponents_AttributeDefinitionId",
                table: "DerivedStatComponents",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricDefinitions_GameSystemId",
                table: "MetricDefinitions",
                column: "GameSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricComponents_MetricDefinitionId",
                table: "MetricComponents",
                column: "MetricDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricComponents_AttributeDefinitionId",
                table: "MetricComponents",
                column: "AttributeDefinitionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DerivedStatComponents");

            migrationBuilder.DropTable(
                name: "MetricComponents");

            migrationBuilder.DropTable(
                name: "DerivedStatDefinitions");

            migrationBuilder.DropTable(
                name: "MetricDefinitions");

            migrationBuilder.DropColumn(
                name: "DefaultValueMode",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "DefaultValueFlatBonus",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "DefaultValueDiceCount",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "DefaultValueDiceSides",
                table: "AttributeDefinitions");
        }
    }
}