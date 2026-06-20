#!/usr/bin/env python3
"""Static checks for the production Admin RBAC permission-policy foundation seam."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

FILES = {
    "authorization_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/AdminAuthorizationConstants.cs",
    "permission_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/AdminPermissionConstants.cs",
    "catalog_service": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRolePermissionCatalogService.cs",
    "admin_endpoints": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs",
    "admin_js": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js",
    "admin_index": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html",
}

PRODUCTION_PERMISSION_POLICIES = {
    "CmsDraftSavePermissionPolicyName": ("CmsContentWriteDraft", "cms.content.write_draft"),
    "CmsPublishPermissionPolicyName": ("CmsContentPublish", "cms.content.publish"),
    "CmsRestorePermissionPolicyName": ("CmsContentRestore", "cms.content.restore"),
    "UserLookupPermissionPolicyName": ("UserLookupRead", "users.lookup.read"),
    "UserOverviewPermissionPolicyName": ("UserOverviewRead", "users.overview.read"),
    "LessonHistoryDiagnosticsPermissionPolicyName": ("LessonHistoryDiagnosticsRead", "lesson_history.diagnostics.read"),
    "PremiumDiagnosticsPermissionPolicyName": ("PremiumDiagnosticsRead", "premium.diagnostics.read"),
    "ManualPremiumGrantPermissionPolicyName": ("PremiumGrant", "premium.grant"),
    "ManualPremiumRevokePermissionPolicyName": ("PremiumRevoke", "premium.revoke"),
    "FreeLessonResetPermissionPolicyName": ("FreeLessonAllowanceReset", "free_lesson_allowance.reset"),
    "BillingCancelRenewalPermissionPolicyName": ("BillingCancelRenewal", "billing.cancel_renewal"),
    "BillingEventDiagnosticsPermissionPolicyName": ("BillingDiagnosticsRead", "billing.diagnostics.read"),
    "AuditLogViewPermissionPolicyName": ("AuditRead", "audit.read"),
    "SystemDiagnosticsPermissionPolicyName": ("SystemDiagnosticsRead", "system.diagnostics.read"),
    "AdminRoleManagementPermissionPolicyName": ("AdminRolesManage", "admin.roles.manage"),
}

DANGEROUS_POLICY_CONSTANTS = [
    "ManualPremiumGrantPermissionPolicyName",
    "ManualPremiumRevokePermissionPolicyName",
    "FreeLessonResetPermissionPolicyName",
    "BillingCancelRenewalPermissionPolicyName",
    "CmsPublishPermissionPolicyName",
    "CmsRestorePermissionPolicyName",
    "AdminRoleManagementPermissionPolicyName",
]

FORBIDDEN_PADDLE_CLIENT_REFERENCES = [
    "api.paddle.com",
    "Paddle.Api",
    "Paddle-Signature",
    "PADDLE_API_KEY",
    "PADDLE_WEBHOOK_SECRET",
    "webhook secret",
]


def read(name: str) -> str:
    path = FILES[name]
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def extract_constant_values(text: str, suffix: str | None = None) -> dict[str, str]:
    values = dict(re.findall(r"public const string (\w+) = \"([^\"]+)\";", text))
    if suffix:
        values = {name: value for name, value in values.items() if name.endswith(suffix)}
    return values


def main() -> None:
    authorization_constants = read("authorization_constants")
    permission_constants = read("permission_constants")
    catalog_service = read("catalog_service")
    admin_endpoints = read("admin_endpoints")
    admin_ui = read("admin_js") + "\n" + read("admin_index")

    policy_values = extract_constant_values(authorization_constants, "PermissionPolicyName")
    if len(policy_values.values()) != len(set(policy_values.values())):
        raise AssertionError("Production permission policy names must be unique.")

    permission_values = extract_constant_values(permission_constants)
    if len(permission_values.values()) != len(set(permission_values.values())):
        raise AssertionError("Admin permission names must be unique.")

    for policy_constant, (permission_constant, permission_name) in PRODUCTION_PERMISSION_POLICIES.items():
        require(permission_constants, f'public const string {permission_constant} = "{permission_name}"', f"permission constant for {permission_name}")
        require(authorization_constants, f'public const string {policy_constant} = "AdminPermission:{permission_name}"', f"policy constant for {permission_name}")
        require(catalog_service, f"AdminPermissionConstants.{permission_constant}", f"BootstrapAdmin catalog includes {permission_name}")

    for policy_constant in DANGEROUS_POLICY_CONSTANTS:
        require(authorization_constants, policy_constant, f"explicit dangerous action policy {policy_constant}")

    require(authorization_constants, 'BootstrapAdminPolicyName = "BootstrapAdmin"', "existing BootstrapAdmin policy constant")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "admin endpoints still use BootstrapAdmin policy")
    for policy_constant in PRODUCTION_PERMISSION_POLICIES:
        forbid(admin_endpoints, f"AdminAuthorizationConstants.{policy_constant}", "production permission policy endpoint enforcement in this foundation-only step")

    for needle in FORBIDDEN_PADDLE_CLIENT_REFERENCES:
        forbid(admin_ui, needle, "direct Paddle reference in Admin UI")

    desktop_files = [
        path for path in ROOT.rglob("*")
        if path.is_file()
        and ".git" not in path.parts
        and "backend" not in path.parts
        and "docs" not in path.parts
        and "tools" not in path.parts
        and path.suffix.lower() in {".cs", ".xaml", ".json", ".xml", ".config"}
    ]
    desktop_text = "\n".join(path.read_text(encoding="utf-8-sig", errors="ignore") for path in desktop_files)
    for needle in FORBIDDEN_PADDLE_CLIENT_REFERENCES:
        forbid(desktop_text, needle, "direct Paddle reference in Desktop code")

    print("Admin RBAC permission policy foundation checks passed.")


if __name__ == "__main__":
    main()
