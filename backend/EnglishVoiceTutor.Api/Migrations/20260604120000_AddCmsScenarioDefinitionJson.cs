using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604120000_AddCmsScenarioDefinitionJson")]
    public partial class AddCmsScenarioDefinitionJson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefinitionJson",
                table: "cms_lesson_scenarios",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefinitionJson",
                table: "cms_lesson_scenarios");
        }
    }
}
