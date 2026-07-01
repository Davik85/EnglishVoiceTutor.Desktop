using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations
{
    public partial class AddAdminAuthAuditEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_auth_audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    AttemptedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    AdminSource = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RoleIdsJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    FailureReasonCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SafeMetadataJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_auth_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_auth_audit_events_admin_users_ActorAdminUserId",
                        column: x => x.ActorAdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_auth_audit_events_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_admin_auth_audit_events_ActorAdminUserId", table: "admin_auth_audit_events", column: "ActorAdminUserId");
            migrationBuilder.CreateIndex(name: "IX_admin_auth_audit_events_ActorUserId", table: "admin_auth_audit_events", column: "ActorUserId");
            migrationBuilder.CreateIndex(name: "IX_admin_auth_audit_events_EventType", table: "admin_auth_audit_events", column: "EventType");
            migrationBuilder.CreateIndex(name: "IX_admin_auth_audit_events_OccurredAtUtc", table: "admin_auth_audit_events", column: "OccurredAtUtc");
            migrationBuilder.CreateIndex(name: "IX_admin_auth_audit_events_Result", table: "admin_auth_audit_events", column: "Result");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "admin_auth_audit_events");
        }
    }
}
