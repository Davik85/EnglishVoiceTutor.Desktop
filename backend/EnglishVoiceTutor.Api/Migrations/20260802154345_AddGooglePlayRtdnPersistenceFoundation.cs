using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGooglePlayRtdnPersistenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "google_play_purchase_token_secrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GooglePlayPurchaseClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseTokenFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPurchaseToken = table.Column<string>(type: "text", nullable: false),
                    ProtectionFormatVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetentionDeleteAfterUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastProviderCheckAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextProviderCheckAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReconciliationAttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastSafeResultCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FinalRecheckUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgementPending = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_play_purchase_token_secrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_google_play_purchase_token_secrets_google_play_purchase_cla~",
                        column: x => x.GooglePlayPurchaseClaimId,
                        principalTable: "google_play_purchase_claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "google_play_rtdn_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PubSubMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PubSubSubscription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PackageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NotificationKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PurchaseTokenFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SafeErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_play_rtdn_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_purchase_token_secrets_GooglePlayPurchaseClaimId",
                table: "google_play_purchase_token_secrets",
                column: "GooglePlayPurchaseClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_purchase_token_secrets_NextProviderCheckAtUtc_R~",
                table: "google_play_purchase_token_secrets",
                columns: new[] { "NextProviderCheckAtUtc", "ReconciliationAttemptCount" });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_purchase_token_secrets_PurchaseTokenFingerprint",
                table: "google_play_purchase_token_secrets",
                column: "PurchaseTokenFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_purchase_token_secrets_SupersededAtUtc_Retentio~",
                table: "google_play_purchase_token_secrets",
                columns: new[] { "SupersededAtUtc", "RetentionDeleteAfterUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_rtdn_events_Provider_PubSubSubscription_PubSubM~",
                table: "google_play_rtdn_events",
                columns: new[] { "Provider", "PubSubSubscription", "PubSubMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_rtdn_events_PurchaseTokenFingerprint_ReceivedAt~",
                table: "google_play_rtdn_events",
                columns: new[] { "PurchaseTokenFingerprint", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_rtdn_events_Status_NextAttemptAtUtc",
                table: "google_play_rtdn_events",
                columns: new[] { "Status", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "google_play_purchase_token_secrets");

            migrationBuilder.DropTable(
                name: "google_play_rtdn_events");
        }
    }
}
