using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaddleWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paddle_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaddleEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaddleNotificationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PaddleTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PaddleSubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PaddleCustomerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    InternalUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InternalPlanId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    SignatureHeader = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paddle_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_EventType",
                table: "paddle_webhook_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_InternalUserId",
                table: "paddle_webhook_events",
                column: "InternalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_PaddleEventId",
                table: "paddle_webhook_events",
                column: "PaddleEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_PaddleSubscriptionId",
                table: "paddle_webhook_events",
                column: "PaddleSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_PaddleTransactionId",
                table: "paddle_webhook_events",
                column: "PaddleTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_ProcessingStatus",
                table: "paddle_webhook_events",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_paddle_webhook_events_ReceivedAtUtc",
                table: "paddle_webhook_events",
                column: "ReceivedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paddle_webhook_events");
        }
    }
}
