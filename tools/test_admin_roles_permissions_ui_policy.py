#!/usr/bin/env python3
"""Static policy checks for Admin Shell roles/permissions UI awareness."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
ADMIN_HTML = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
AUTH_ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs"
ADMIN_ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Unexpected {label}: {needle}")


def main() -> None:
    admin_js = read(ADMIN_JS)
    admin_html = read(ADMIN_HTML)
    auth_endpoints = read(AUTH_ENDPOINTS)
    admin_endpoints = read(ADMIN_ENDPOINTS)
    combined = admin_js + "\n" + admin_html

    assert_contains(admin_js, 'adminMe: "/api/admin/me"', "Admin Shell /api/admin/me path")
    assert_contains(admin_js, "roles", "roles storage/rendering")
    assert_contains(admin_js, "permissions", "permissions storage/rendering")
    assert_contains(admin_js, "isBootstrapAdmin", "BootstrapAdmin state")
    assert_contains(admin_js, "productionRolesAvailable", "production roles availability state")

    for permission_id in [
        "users.read",
        "premium.grant",
        "cms.content.read",
        "cms.content.publish",
        "cms.runtime_status.read",
        "product_statistics.read",
    ]:
        assert_contains(admin_js, permission_id, f"workflow permission id {permission_id}")

    assert_contains(combined, "Production role management is a controlled persistent-RBAC workflow", "production role management controlled wording")
    assert_contains(combined, "server cutover status", "server cutover status wording")
    assert_contains(combined, "Production RBAC", "production RBAC status wording")
    assert_contains(admin_html, "linked persistent Admin User role", "persistent Admin User sign-in wording")
    assert_not_contains(admin_html, "Development bootstrap admin account", "stale bootstrap-only sign-in wording")

    assert_contains(auth_endpoints, "HasPersistentAdminShellAccessAsync", "persistent Admin User login gate")
    assert_contains(auth_endpoints, "AdminPermissionConstants.AdminSelfRead", "admin shell self-read login permission")
    assert_contains(auth_endpoints, "GetEffectiveRolesByUserIdAsync", "persistent role login by linked app user id")
    assert_contains(auth_endpoints, "GetEffectiveRolesByNormalizedEmailAsync", "persistent role login by normalized email fallback")
    assert_contains(admin_endpoints, "persistent_role_assignment", "persistent Admin User /api/admin/me source")
    assert_contains(admin_endpoints, "ResolvePermissions", "persistent Admin User /api/admin/me permissions")

    # This UI-awareness step must remain informational: do not gate tabs, buttons, or fetches on permission checks.
    assert_not_contains(admin_js, "hasPermission", "client-side permission enforcement helper")
    assert_contains(admin_js, "renderWorkflowAvailability", "informational workflow permission rendering")
    assert_not_contains(admin_js, "tabButtons.filter", "tab hiding based on permissions")
    assert_not_contains(admin_js, "data-permission", "permission-bound UI controls")

    # Billing/Paddle production readiness must remain unavailable/deferred.
    assert_not_contains(combined, "PaddleCheckoutAvailable = true", "Paddle checkout production enablement")
    assert_not_contains(combined, "PaddleWebhooksAvailable = true", "Paddle webhook production enablement")
    assert_not_contains(combined, "billing/Paddle production", "billing/Paddle production enablement wording")

    print("Admin roles/permissions UI policy checks passed.")


if __name__ == "__main__":
    main()
