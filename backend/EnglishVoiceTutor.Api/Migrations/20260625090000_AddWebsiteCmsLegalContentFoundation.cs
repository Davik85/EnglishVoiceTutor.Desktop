using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteCmsLegalContentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "website_cms_sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DraftBody = table.Column<string>(type: "text", nullable: false),
                    PublishedBody = table.Column<string>(type: "text", nullable: true),
                    ReviewStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_website_cms_sections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_website_cms_sections_ReviewStatus",
                table: "website_cms_sections",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_website_cms_sections_SectionKey",
                table: "website_cms_sections",
                column: "SectionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "website_cms_sections");
        }
    }
}
