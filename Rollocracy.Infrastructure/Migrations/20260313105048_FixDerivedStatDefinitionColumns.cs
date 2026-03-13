using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class FixDerivedStatDefinitionColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinValue",
                table: "DerivedStatDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxValue",
                table: "DerivedStatDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 100);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "DerivedStatDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "DerivedStatDefinitions");
        }
    }
}