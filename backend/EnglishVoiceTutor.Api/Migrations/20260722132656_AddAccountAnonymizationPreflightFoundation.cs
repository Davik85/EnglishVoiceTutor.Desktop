using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAnonymizationPreflightFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_anonymization_policy_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VersionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CategoryDecisionsJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_anonymization_policy_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_anonymization_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreflightVersion = table.Column<int>(type: "integer", nullable: false),
                    PreflightFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcedureVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CategoryCountsJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    BlockingCodesJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    RetentionSummaryJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    ProviderStatesJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    BackupReconciliationState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyRevision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_anonymization_operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_anonymization_operations_account_anonymization_poli~",
                        column: x => x.PolicySnapshotId,
                        principalTable: "account_anonymization_policy_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_anonymization_operations_admin_users_ActorAdminUser~",
                        column: x => x.ActorAdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_anonymization_operations_user_feedback_reports_Repo~",
                        column: x => x.ReportId,
                        principalTable: "user_feedback_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_anonymization_operations_users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_operations_ActorAdminUserId",
                table: "account_anonymization_operations",
                column: "ActorAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_operations_PolicySnapshotId",
                table: "account_anonymization_operations",
                column: "PolicySnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_operations_ReportId",
                table: "account_anonymization_operations",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_operations_State_UpdatedAtUtc",
                table: "account_anonymization_operations",
                columns: new[] { "State", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_operations_TargetUserId",
                table: "account_anonymization_operations",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_policy_snapshots_PolicyVersion",
                table: "account_anonymization_policy_snapshots",
                column: "PolicyVersion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_anonymization_policy_snapshots_VersionHash",
                table: "account_anonymization_policy_snapshots",
                column: "VersionHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_anonymization_operations");

            migrationBuilder.DropTable(
                name: "account_anonymization_policy_snapshots");
        }
    }
}
