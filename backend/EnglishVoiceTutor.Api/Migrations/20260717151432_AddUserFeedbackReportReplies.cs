using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFeedbackReportReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_feedback_report_replies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplyText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_feedback_report_replies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_feedback_report_replies_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_feedback_report_replies_user_feedback_reports_Feedback~",
                        column: x => x.FeedbackReportId,
                        principalTable: "user_feedback_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_feedback_report_replies_AdminUserId",
                table: "user_feedback_report_replies",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_feedback_report_replies_FeedbackReportId",
                table: "user_feedback_report_replies",
                column: "FeedbackReportId");

            migrationBuilder.CreateIndex(
                name: "IX_user_feedback_report_replies_FeedbackReportId_CreatedAtUtc",
                table: "user_feedback_report_replies",
                columns: new[] { "FeedbackReportId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_feedback_report_replies");
        }
    }
}
