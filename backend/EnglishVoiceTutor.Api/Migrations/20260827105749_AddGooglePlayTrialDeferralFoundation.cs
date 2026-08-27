using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGooglePlayTrialDeferralFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "google_play_initial_premium_deferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GooglePlayPurchaseClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderPurchaseStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BaselineProviderExpiryUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExistingCoverageStartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExistingCoverageTailUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLicenseTestPurchase = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedDeferDurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    TargetProviderExpiryUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommandEtag = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSafeErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderResponseExpiryUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthoritativeProviderExpiryUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyRevision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_play_initial_premium_deferrals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_initial_premium_deferrals_GooglePlayPurchaseClaimId",
                table: "google_play_initial_premium_deferrals",
                column: "GooglePlayPurchaseClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_play_initial_premium_deferrals_Status_NextAttemptAtUtc",
                table: "google_play_initial_premium_deferrals",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_google_play_initial_premium_deferrals_UserId",
                table: "google_play_initial_premium_deferrals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "google_play_initial_premium_deferrals");
        }
    }
}
