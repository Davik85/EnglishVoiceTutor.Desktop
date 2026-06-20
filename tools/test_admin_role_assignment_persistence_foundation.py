#!/usr/bin/env python3
"""Static checks for the admin role assignment persistence foundation."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

APP_DB_CONTEXT = ROOT / "backend/EnglishVoiceTutor.Api/Data/AppDbContext.cs"
ENTITY_CONSTANTS = ROOT / "backend/EnglishVoiceTutor.Api/Data/EntityConstants.cs"
ENTITIES = ROOT / "backend/EnglishVoiceTutor.Api/Data/Entities"
MIGRATION = ROOT / "backend/EnglishVoiceTutor.Api/Migrations/20260620120000_AddAdminRoleAssignmentPersistence.cs"
ADMIN_HANDLER = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminPermissionAuthorizationHandler.cs"
ADMIN_ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"
DESKTOP_ROOT = ROOT
ADMIN_UI_ROOT = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin"

EXPECTED_TABLES = {
    "admin_users",
    "admin_user_roles",
    "admin_role_assignment_events",
}
FORBIDDEN_TABLE_TERMS = [
    "entitlement",
    "subscription",
    "billing",
    "payment",
    "paddle",
    "lesson",
    "daily_free_lesson",
    "desktop",
]
MIGRATED_POLICY_CONSTANTS = {
    "AdminSelfReadPermissionPolicyName",
    "AdminCapabilitiesReadPermissionPolicyName",
    "ProductStatisticsReadPermissionPolicyName",
}
BOOTSTRAP_REQUIRED_ROUTES = {
    "AdminUserByEmailRoute",
    "AdminUserByIdRoute",
    "AdminUserAuditActionsRoute",
    "AdminUserPremiumGrantsRoute",
    "AdminUserPremiumGrantRevokeRoute",
    "AdminUserFreeLessonAllowanceResetRoute",
    "AdminUserBillingCancelRenewalRoute",
    "AdminDevCmsContentPackPublishRoute",
    "AdminDevCmsContentPackVersionRestoreRoute",
}


def read(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def list_source_files(root: Path) -> list[Path]:
    return [path for path in root.rglob("*") if path.is_file() and path.suffix.lower() in {".cs", ".xaml", ".js", ".html", ".ts", ".tsx", ".jsx"}]


def main() -> None:
    app_db_context = read(APP_DB_CONTEXT)
    entity_constants = read(ENTITY_CONSTANTS)
    migration = read(MIGRATION)
    admin_handler = read(ADMIN_HANDLER)
    admin_endpoints = read(ADMIN_ENDPOINTS)

    for entity_name in ["AdminUserEntity", "AdminUserRoleEntity", "AdminRoleAssignmentEventEntity"]:
        entity_text = read(ENTITIES / f"{entity_name}.cs")
        require(entity_text, f"public sealed class {entity_name}", f"{entity_name} declaration")
        require(app_db_context, f"DbSet<{entity_name}>", f"{entity_name} DbSet")

    for constant_name, table_name in [
        ("AdminUsers", "admin_users"),
        ("AdminUserRoles", "admin_user_roles"),
        ("AdminRoleAssignmentEvents", "admin_role_assignment_events"),
    ]:
        require(entity_constants, f'public const string {constant_name} = "{table_name}";', f"{table_name} table constant")
        require(app_db_context, f"EntityConstants.TableNames.{constant_name}", f"{table_name} mapping")
        require(migration, f'name: "{table_name}"', f"{table_name} migration table")

    created_tables = set(re.findall(r'CreateTable\(\s*\n\s*name: "([^"]+)"', migration))
    if created_tables != EXPECTED_TABLES:
        raise AssertionError(f"Migration must create only admin role assignment persistence tables. Found: {sorted(created_tables)}")

    if re.search(r'InsertData\(|UpdateData\(|DeleteData\(', migration):
        raise AssertionError("Migration must not seed, update, or delete data.")
    if re.search(r'[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}', migration):
        raise AssertionError("Migration must not contain real admin emails or users.")
    for forbidden in FORBIDDEN_TABLE_TERMS:
        if re.search(rf'name: "[^"]*{forbidden}[^"]*"', migration, flags=re.IGNORECASE):
            raise AssertionError(f"Migration must not create or alter {forbidden} tables.")

    require(app_db_context, '.HasFilter("\\"RevokedAtUtc\\" IS NULL")', "filtered unique active role assignment index")
    for index_snippet in [
        "adminUser.UserId",
        "adminUser.NormalizedEmail",
        "adminUser.Status",
        "role.AdminUserId",
        "role.RoleId",
        "role.AssignedByAdminUserId",
        "role.RevokedAtUtc",
        "roleEvent.ActorAdminUserId",
        "roleEvent.TargetAdminUserId",
        "roleEvent.RoleId",
        "roleEvent.ActionType",
        "roleEvent.Result",
        "roleEvent.OccurredAtUtc",
    ]:
        require(app_db_context, f"HasIndex({index_snippet.split('.')[0]} => {index_snippet})", f"index for {index_snippet}")

    for forbidden in ["AdminUsers", "AdminUserRoles", "AdminRoleAssignmentEvents", "admin_users", "admin_user_roles", "admin_role_assignment_events"]:
        if forbidden in admin_handler:
            raise AssertionError("AdminPermissionAuthorizationHandler must not read persistent admin role assignment tables yet.")

    endpoint_authorizations = re.findall(
        r"app\.Map(Get|Post|Put|Delete)\(ApiConstants\.(Admin\w+Route),\s*[^)]*\)\s*\.RequireAuthorization\(AdminAuthorizationConstants\.(\w+)\)",
        admin_endpoints,
        flags=re.MULTILINE,
    )
    permission_migrated = {(method.upper(), route, policy) for method, route, policy in endpoint_authorizations if policy.endswith("PermissionPolicyName")}
    if {policy for _, _, policy in permission_migrated} != MIGRATED_POLICY_CONSTANTS or len(permission_migrated) != 3:
        raise AssertionError(f"Exactly three safe read-only Admin endpoints must remain permission-policy migrated. Found: {sorted(permission_migrated)}")

    route_to_policy = {route: policy for _, route, policy in endpoint_authorizations}
    for route in BOOTSTRAP_REQUIRED_ROUTES:
        if route_to_policy.get(route) != "BootstrapAdminPolicyName":
            raise AssertionError(f"Dangerous/write/billing/CMS/Premium/free-lesson/user-level endpoint must remain BootstrapAdmin: {route}")

    desktop_text = "\n".join(read(path) for path in list_source_files(DESKTOP_ROOT) if "backend/EnglishVoiceTutor.Api" not in path.as_posix())
    admin_ui_text = "\n".join(read(path) for path in list_source_files(ADMIN_UI_ROOT))
    for label, text in [("Desktop", desktop_text), ("Admin UI", admin_ui_text)]:
        for forbidden in ["api.paddle.com", "Paddle.Api", "PADDLE_API_KEY", "PADDLE_WEBHOOK_SECRET"]:
            if forbidden in text:
                raise AssertionError(f"{label} must not reference Paddle directly: {forbidden}")

    print("Admin role assignment persistence foundation static checks passed.")


if __name__ == "__main__":
    main()
