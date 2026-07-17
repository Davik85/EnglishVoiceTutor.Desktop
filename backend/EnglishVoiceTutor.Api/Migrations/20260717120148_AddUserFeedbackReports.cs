using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFeedbackReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_feedback_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ReportedAiText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientPlatform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClientVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_feedback_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_feedback_reports_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_feedback_reports_CreatedAtUtc",
                table: "user_feedback_reports",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_user_feedback_reports_UserId",
                table: "user_feedback_reports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_feedback_reports");
        }
    }
}
