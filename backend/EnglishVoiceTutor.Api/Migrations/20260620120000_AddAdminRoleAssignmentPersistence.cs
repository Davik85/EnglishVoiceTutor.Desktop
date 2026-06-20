using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishVoiceTutor.Api.Migrations;

public partial class AddAdminRoleAssignmentPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "admin_users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DisabledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_admin_users", x => x.Id);
                table.ForeignKey(
                    name: "FK_admin_users_admin_users_CreatedByAdminUserId",
                    column: x => x.CreatedByAdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_admin_users_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "admin_role_assignment_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                TargetAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ActionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RoleId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Reason = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                OldRolesJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                NewRolesJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SafeMetadataJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_admin_role_assignment_events", x => x.Id);
                table.ForeignKey(
                    name: "FK_admin_role_assignment_events_admin_users_ActorAdminUserId",
                    column: x => x.ActorAdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_admin_role_assignment_events_admin_users_TargetAdminUserId",
                    column: x => x.TargetAdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "admin_user_roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                AssignedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RevokedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                RevokeReason = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_admin_user_roles", x => x.Id);
                table.ForeignKey(
                    name: "FK_admin_user_roles_admin_users_AdminUserId",
                    column: x => x.AdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_admin_user_roles_admin_users_AssignedByAdminUserId",
                    column: x => x.AssignedByAdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_admin_user_roles_admin_users_RevokedByAdminUserId",
                    column: x => x.RevokedByAdminUserId,
                    principalTable: "admin_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_ActionType", table: "admin_role_assignment_events", column: "ActionType");
        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_ActorAdminUserId", table: "admin_role_assignment_events", column: "ActorAdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_OccurredAtUtc", table: "admin_role_assignment_events", column: "OccurredAtUtc");
        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_Result", table: "admin_role_assignment_events", column: "Result");
        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_RoleId", table: "admin_role_assignment_events", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_admin_role_assignment_events_TargetAdminUserId", table: "admin_role_assignment_events", column: "TargetAdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_AdminUserId", table: "admin_user_roles", column: "AdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_AdminUserId_RoleId", table: "admin_user_roles", columns: new[] { "AdminUserId", "RoleId" }, unique: true, filter: "\"RevokedAtUtc\" IS NULL");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_AssignedByAdminUserId", table: "admin_user_roles", column: "AssignedByAdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_RevokedAtUtc", table: "admin_user_roles", column: "RevokedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_RevokedByAdminUserId", table: "admin_user_roles", column: "RevokedByAdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_user_roles_RoleId", table: "admin_user_roles", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_admin_users_CreatedByAdminUserId", table: "admin_users", column: "CreatedByAdminUserId");
        migrationBuilder.CreateIndex(name: "IX_admin_users_NormalizedEmail", table: "admin_users", column: "NormalizedEmail");
        migrationBuilder.CreateIndex(name: "IX_admin_users_Status", table: "admin_users", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_admin_users_UserId", table: "admin_users", column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "admin_role_assignment_events");
        migrationBuilder.DropTable(name: "admin_user_roles");
        migrationBuilder.DropTable(name: "admin_users");
    }
}
