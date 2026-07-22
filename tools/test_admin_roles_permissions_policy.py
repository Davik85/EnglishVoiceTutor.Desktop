#!/usr/bin/env python3
"""Static policy checks for the provider-neutral admin roles/permissions foundation."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

FILES = {
    "role_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/AdminRoleConstants.cs",
    "permission_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/AdminPermissionConstants.cs",
    "catalog_service": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRolePermissionCatalogService.cs",
    "catalog_interface": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRolePermissionCatalogService.cs",
    "admin_me": ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminMeResponse.cs",
    "admin_capabilities": ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminCapabilitiesResponse.cs",
    "capabilities_service": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminCapabilitiesService.cs",
    "program": ROOT / "backend/EnglishVoiceTutor.Api/Program.cs",
    "admin_endpoints": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs",
    "admin_js": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js",
    "admin_index": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html",
}

ROLE_CONSTANTS = {
    "SuperAdmin": "super_admin",
    "Support": "support",
    "ContentEditor": "content_editor",
    "BillingSupport": "billing_support",
    "ReadOnlyAuditor": "read_only_auditor",
}

PERMISSIONS = {
    "AdminSelfRead": "admin.self.read",
    "AdminCapabilitiesRead": "admin.capabilities.read",
    "UsersRead": "users.read",
    "UserLookupRead": "users.lookup.read",
    "UserOverviewRead": "users.overview.read",
    "UsersDiagnosticsRead": "users.diagnostics.read",
    "LessonHistoryDiagnosticsRead": "lesson_history.diagnostics.read",
    "AuditRead": "audit.read",
    "CmsContentRead": "cms.content.read",
    "CmsContentWriteDraft": "cms.content.write_draft",
    "CmsContentPublish": "cms.content.publish",
    "CmsContentRestore": "cms.content.restore",
    "CmsRuntimeStatusRead": "cms.runtime_status.read",
    "SubscriptionsDiagnosticsRead": "subscriptions.diagnostics.read",
    "PremiumDiagnosticsRead": "premium.diagnostics.read",
    "PremiumGrant": "premium.grant",
    "PremiumRevoke": "premium.revoke",
    "FreeLessonAllowanceReset": "free_lesson_allowance.reset",
    "BillingCancelRenewal": "billing.cancel_renewal",
    "BillingDiagnosticsRead": "billing.diagnostics.read",
    "ProductStatisticsRead": "product_statistics.read",
    "SystemDiagnosticsRead": "system.diagnostics.read",
    "AdminRolesManage": "admin.roles.manage",
    "FeedbackReportsRead": "feedback_reports.read",
    "FeedbackReportsStatusManage": "feedback_reports.status.manage",
    "FeedbackReportsReply": "feedback_reports.reply",
    "AccountAnonymizationPreflightRead": "account_anonymization.preflight.read",
    "AccountAnonymizationExecute": "account_anonymization.execute",
}

EXPECTED_ROLE_PERMISSIONS = {
    "SuperAdmin": set(PERMISSIONS),
    "Support": {
        "AdminSelfRead", "AdminCapabilitiesRead", "UsersRead", "UserLookupRead", "UserOverviewRead",
        "UsersDiagnosticsRead", "LessonHistoryDiagnosticsRead", "AuditRead", "FreeLessonAllowanceReset", "SystemDiagnosticsRead",
        "FeedbackReportsRead", "FeedbackReportsStatusManage", "FeedbackReportsReply",
    },
    "ContentEditor": {"AdminSelfRead", "AdminCapabilitiesRead", "CmsContentRead", "CmsContentWriteDraft", "CmsRuntimeStatusRead"},
    "BillingSupport": {
        "AdminSelfRead", "AdminCapabilitiesRead", "UserLookupRead", "UserOverviewRead",
        "SubscriptionsDiagnosticsRead", "PremiumDiagnosticsRead", "PremiumGrant", "BillingDiagnosticsRead", "BillingCancelRenewal",
    },
    "ReadOnlyAuditor": {
        "AdminSelfRead", "AdminCapabilitiesRead", "AuditRead", "UsersDiagnosticsRead", "LessonHistoryDiagnosticsRead",
        "SubscriptionsDiagnosticsRead", "PremiumDiagnosticsRead", "BillingDiagnosticsRead", "ProductStatisticsRead", "SystemDiagnosticsRead",
    },
}

DANGEROUS_FOR_SUPPORT = {"PremiumGrant", "PremiumRevoke", "CmsContentPublish", "CmsContentRestore", "BillingCancelRenewal", "AdminRolesManage"}
CONTENT_EDITOR_FORBIDDEN_PREFIXES = ("Billing", "Premium", "AdminRoles")
BILLING_SUPPORT_FORBIDDEN = {"CmsContentWriteDraft", "CmsContentPublish", "CmsContentRestore", "AdminRolesManage", "PremiumRevoke"}
READ_ONLY_ACTIONS = {
    "CmsContentWriteDraft", "CmsContentPublish", "CmsContentRestore", "PremiumGrant", "PremiumRevoke",
    "FreeLessonAllowanceReset", "BillingCancelRenewal", "AdminRolesManage",
}
FORBIDDEN_PADDLE_CLIENT_REFERENCES = ["api.paddle.com", "Paddle.Api", "Paddle-Signature", "PADDLE_API_KEY", "PADDLE_WEBHOOK_SECRET", "webhook secret"]


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


def extract_role_block(catalog_service: str, role_constant: str) -> set[str]:
    pattern = rf"\[AdminRoleConstants\.{role_constant}\]\s*=\s*(?:BootstrapAdminPermissions|\[(.*?)\])"
    match = re.search(pattern, catalog_service, re.S)
    if not match:
        raise AssertionError(f"Missing production catalog entry for {role_constant}")
    if match.group(0).endswith("BootstrapAdminPermissions"):
        return set(PERMISSIONS)
    return set(re.findall(r"AdminPermissionConstants\.(\w+)", match.group(1)))


def main() -> None:
    role_constants = read("role_constants")
    permission_constants = read("permission_constants")
    catalog_service = read("catalog_service")
    catalog_interface = read("catalog_interface")
    capabilities_service = read("capabilities_service")
    program = read("program")
    admin_endpoints = read("admin_endpoints")
    admin_ui = read("admin_js") + "\n" + read("admin_index")

    for constant_name, role_id in ROLE_CONSTANTS.items():
        require(role_constants, f'public const string {constant_name} = "{role_id}"', f"stable production admin role {role_id}")
        require(catalog_service, f"AdminRoleConstants.{constant_name}", f"production catalog role {role_id}")

    for constant_name, permission_id in PERMISSIONS.items():
        require(permission_constants, f'public const string {constant_name} = "{permission_id}"', f"stable admin permission ID {permission_id}")
        require(catalog_service, f"AdminPermissionConstants.{constant_name}", f"catalog permission {permission_id}")

    require(catalog_interface, "GetProductionRolePermissions()", "static production role-permission catalog method")
    require(catalog_service, "GetProductionRolePermissions() => ProductionRolePermissions", "production role catalog returned")
    require(catalog_service, "[AdminRoleConstants.SuperAdmin] = BootstrapAdminPermissions", "Super Admin includes all production permissions")

    for role_constant, expected_permissions in EXPECTED_ROLE_PERMISSIONS.items():
        actual_permissions = extract_role_block(catalog_service, role_constant)
        unknown_permissions = actual_permissions - set(PERMISSIONS)
        if unknown_permissions:
            raise AssertionError(f"{role_constant} maps unknown permissions: {sorted(unknown_permissions)}")
        if actual_permissions != expected_permissions:
            raise AssertionError(f"{role_constant} permissions mismatch. Expected {sorted(expected_permissions)}, got {sorted(actual_permissions)}")

    if extract_role_block(catalog_service, "Support") & DANGEROUS_FOR_SUPPORT:
        raise AssertionError("Support includes dangerous permissions")
    if any(permission.startswith(CONTENT_EDITOR_FORBIDDEN_PREFIXES) for permission in extract_role_block(catalog_service, "ContentEditor")):
        raise AssertionError("Content Editor includes billing/Premium/admin-role permissions")
    if extract_role_block(catalog_service, "BillingSupport") & BILLING_SUPPORT_FORBIDDEN:
        raise AssertionError("Billing Support includes CMS write/publish/restore, Premium revoke, or admin-role permissions")
    if extract_role_block(catalog_service, "ReadOnlyAuditor") & READ_ONLY_ACTIONS:
        raise AssertionError("Read-only Auditor includes write/action permissions")

    require(catalog_service, "AdminRoleConstants.SuperAdmin", "bootstrap admin maps to super_admin")
    require(catalog_service, "GetBootstrapAdminRoles() => BootstrapAdminRoles", "bootstrap admin roles returned from catalog")
    require(catalog_service, "GetBootstrapAdminPermissions() => BootstrapAdminPermissions", "bootstrap admin permissions returned from catalog")
    require(capabilities_service, "ProductionRolesAvailable = productionRolesAvailable", "production roles reflect RBAC cutover status")
    require(capabilities_service, "BootstrapAdminFallbackForAdminPermissionPoliciesEnabled: false", "production roles require fallback disabled")
    require(capabilities_service, "BootstrapAdminFallbackConfigurationValuePresent: true", "production roles require explicit fallback configuration")
    require(program, "AddSingleton<IAdminRolePermissionCatalogService, AdminRolePermissionCatalogService>()", "catalog service registration")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "non-migrated admin endpoints still use BootstrapAdmin policy")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AdminSelfReadPermissionPolicyName)", "admin identity endpoint uses AdminSelfRead permission policy")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AdminCapabilitiesReadPermissionPolicyName)", "admin capabilities endpoint uses AdminCapabilitiesRead permission policy")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.ProductStatisticsReadPermissionPolicyName)", "admin product statistics overview endpoint uses ProductStatisticsRead permission policy")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.CmsRuntimeStatusReadPermissionPolicyName)", "admin CMS runtime status endpoint uses CmsRuntimeStatusRead permission policy")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminDevCmsContentPacksRoute, ListCmsContentPacksAsync)", "admin CMS content packs list endpoint is GET-only")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.CmsContentReadPermissionPolicyName)", "admin CMS content read endpoints use CmsContentRead permission policy")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AuditLogViewPermissionPolicyName)", "admin CMS audit read endpoints use AuditRead permission policy")
    migrated_permission_authorizations = re.findall(r"RequireAuthorization\(AdminAuthorizationConstants\.(\w+PermissionPolicyName)\)", admin_endpoints)
    expected_permission_authorizations = [
        "AdminSelfReadPermissionPolicyName", "AdminCapabilitiesReadPermissionPolicyName", "ProductStatisticsReadPermissionPolicyName",
        "UserLookupPermissionPolicyName", "UserOverviewPermissionPolicyName", "ManualPremiumGrantPermissionPolicyName", "ManualPremiumRevokePermissionPolicyName", "AuditLogViewPermissionPolicyName", "AuditLogViewPermissionPolicyName",
        "FreeLessonResetPermissionPolicyName", "BillingCancelRenewalPermissionPolicyName",
        "CmsContentReadPermissionPolicyName", "CmsRuntimeStatusReadPermissionPolicyName", "CmsRuntimeStatusReadPermissionPolicyName",
        "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName",
        "CmsDraftSavePermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName",
        "CmsDraftSavePermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName",
        "CmsDraftSavePermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName",
        "CmsDraftSavePermissionPolicyName", "AuditLogViewPermissionPolicyName", "AuditLogViewPermissionPolicyName",
        "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName", "CmsContentReadPermissionPolicyName",
        "CmsContentReadPermissionPolicyName", "CmsPublishPermissionPolicyName", "CmsRestorePermissionPolicyName",
    ]
    existing_endpoint_authorizations = [
        policy for policy in migrated_permission_authorizations
        if policy != "AdminRoleManagementPermissionPolicyName"
    ]
    if existing_endpoint_authorizations != expected_permission_authorizations:
        raise AssertionError(f"Exactly thirty-six endpoints may use permission policies after intentionally adding read-only Admin Activity as the 36th AuditRead endpoint. Got: {existing_endpoint_authorizations}")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminActivityRoute, GetAdminActivityAsync)", "intentional read-only Admin Activity endpoint")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminUserAuditActionsRoute, GetTargetUserAuditActionsAsync)", "existing target-user Audit Log endpoint remains present")
    if re.search(r"app\.Map(Post|Put|Delete)\(ApiConstants\.AdminActivityRoute", admin_endpoints):
        raise AssertionError("Admin Activity must remain read-only and must not expose POST, PUT, or DELETE mappings.")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminRoleAssignmentDiagnosticsRoute, GetAdminRoleAssignmentDiagnosticsAsync)", "new role assignment diagnostics endpoint")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "new role assignment diagnostics endpoint uses role-management permission")
    forbid(read("program"), "GetProductionRolePermissions()", "production role catalog endpoint enforcement")

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

    print("Admin roles/permissions policy checks passed.")


if __name__ == "__main__":
    main()
