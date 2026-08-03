using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGooglePlayPendingRefundReviewFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "google_play_pending_refund_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PubSubMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PackageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PendingRefundTokenFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrderIdFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedReviewPayload = table.Column<string>(type: "text", nullable: true),
                    ProtectionFormatVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NotificationVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RefundReason = table.Column<int>(type: "integer", nullable: false),
                    EventTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewDeadlineAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSafeResultCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RefundPreference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SampleContentProvided = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProtectedPayloadDeleteAfterUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_play_pending_refund_reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_pending_refund_reviews_OrderIdFingerprint",
                table: "google_play_pending_refund_reviews",
                column: "OrderIdFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_google_play_pending_refund_reviews_PendingRefundTokenFinger~",
                table: "google_play_pending_refund_reviews",
                column: "PendingRefundTokenFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_pending_refund_reviews_ProtectedPayloadDeleteAf~",
                table: "google_play_pending_refund_reviews",
                column: "ProtectedPayloadDeleteAfterUtc");

            migrationBuilder.CreateIndex(
                name: "IX_google_play_pending_refund_reviews_PubSubMessageId",
                table: "google_play_pending_refund_reviews",
                column: "PubSubMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_pending_refund_reviews_Status_NextAttemptAtUtc",
                table: "google_play_pending_refund_reviews",
                columns: new[] { "Status", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "google_play_pending_refund_reviews");
        }
    }
}
