using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604121000_AddCmsDraftSaveAuditMetadata")]
    public partial class AddCmsDraftSaveAuditMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorEmail",
                table: "cms_content_audit_logs",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentPackSlug",
                table: "cms_content_audit_logs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "cms_content_audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StableKey",
                table: "cms_content_audit_logs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "cms_content_audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_audit_logs_ContentPackSlug_CreatedAtUtc",
                table: "cms_content_audit_logs",
                columns: new[] { "ContentPackSlug", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_audit_logs_EntityType_CreatedAtUtc",
                table: "cms_content_audit_logs",
                columns: new[] { "EntityType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_content_audit_logs_StableKey_CreatedAtUtc",
                table: "cms_content_audit_logs",
                columns: new[] { "StableKey", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cms_content_audit_logs_ContentPackSlug_CreatedAtUtc",
                table: "cms_content_audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_cms_content_audit_logs_EntityType_CreatedAtUtc",
                table: "cms_content_audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_cms_content_audit_logs_StableKey_CreatedAtUtc",
                table: "cms_content_audit_logs");

            migrationBuilder.DropColumn(
                name: "ActorEmail",
                table: "cms_content_audit_logs");

            migrationBuilder.DropColumn(
                name: "ContentPackSlug",
                table: "cms_content_audit_logs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "cms_content_audit_logs");

            migrationBuilder.DropColumn(
                name: "StableKey",
                table: "cms_content_audit_logs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "cms_content_audit_logs");
        }
    }
}
