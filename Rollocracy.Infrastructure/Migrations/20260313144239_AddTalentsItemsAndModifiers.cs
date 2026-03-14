
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rollocracy.Infrastructure.Migrations
{
    public partial class AddTalentsItemsAndModifiers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TalentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GameSystemId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    DisplayOrder = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GameSystemId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    DisplayOrder = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TalentModifierDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TalentDefinitionId = table.Column<Guid>(nullable: false),
                    TargetType = table.Column<int>(nullable: false),
                    TargetId = table.Column<Guid>(nullable: false),
                    AddValue = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentModifierDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemModifierDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ItemDefinitionId = table.Column<Guid>(nullable: false),
                    TargetType = table.Column<int>(nullable: false),
                    TargetId = table.Column<Guid>(nullable: false),
                    AddValue = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemModifierDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTalents",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<Guid>(nullable: false),
                    TalentDefinitionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTalents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<Guid>(nullable: false),
                    ItemDefinitionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterItems", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TalentDefinitions");
            migrationBuilder.DropTable(name: "ItemDefinitions");
            migrationBuilder.DropTable(name: "TalentModifierDefinitions");
            migrationBuilder.DropTable(name: "ItemModifierDefinitions");
            migrationBuilder.DropTable(name: "CharacterTalents");
            migrationBuilder.DropTable(name: "CharacterItems");
        }
    }
}
