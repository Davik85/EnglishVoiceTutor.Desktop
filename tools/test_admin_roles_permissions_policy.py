#!/usr/bin/env python3
"""Static policy checks for the provider-neutral admin roles/permissions foundation."""
from __future__ import annotations

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
}

ROLE_IDS = [
    "super_admin",
    "support_agent",
    "content_manager",
    "finance_admin",
    "readonly_analyst",
]

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
}

BILLING_ACTIVATION_SNIPPETS = [
    "ProductionRolesAvailable = true",
    "BillingProviderConfigured = true",
    "PaddleCheckoutAvailable = true",
    "PaddleWebhooksAvailable = true",
    "MobileStoreEntitlementBridgeAvailable = true",
]


def read(name: str) -> str:
    path = FILES[name]
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    role_constants = read("role_constants")
    permission_constants = read("permission_constants")
    catalog_service = read("catalog_service")
    catalog_interface = read("catalog_interface")
    admin_me = read("admin_me")
    admin_capabilities = read("admin_capabilities")
    capabilities_service = read("capabilities_service")
    program = read("program")

    for role_id in ROLE_IDS:
        assert_contains(role_constants, f'"{role_id}"', f"stable admin role ID {role_id}")

    for constant_name, permission_id in PERMISSIONS.items():
        assert_contains(permission_constants, f'public const string {constant_name} = "{permission_id}"', f"stable admin permission ID {permission_id}")
        assert_contains(catalog_service, f"AdminPermissionConstants.{constant_name}", f"bootstrap catalog permission {permission_id}")

    assert_contains(catalog_interface, "GetBootstrapAdminRoles()", "bootstrap role catalog method")
    assert_contains(catalog_interface, "GetBootstrapAdminPermissions()", "bootstrap permission catalog method")
    assert_contains(catalog_service, "AdminRoleConstants.SuperAdmin", "bootstrap admin maps to super_admin")
    assert_contains(catalog_service, "GetBootstrapAdminRoles() => BootstrapAdminRoles", "bootstrap admin roles returned from catalog")
    assert_contains(catalog_service, "GetBootstrapAdminPermissions() => BootstrapAdminPermissions", "bootstrap admin permissions returned from catalog")

    for property_name in ["Roles", "Permissions", "IsBootstrapAdmin"]:
        assert_contains(admin_me, property_name, f"AdminMeResponse {property_name}")

    for property_name in ["Roles", "Permissions"]:
        assert_contains(admin_capabilities, property_name, f"AdminCapabilitiesResponse {property_name}")

    assert_contains(capabilities_service, "Roles = adminRolePermissionCatalogService.GetBootstrapAdminRoles()", "capabilities exposes roles")
    assert_contains(capabilities_service, "Permissions = adminRolePermissionCatalogService.GetBootstrapAdminPermissions()", "capabilities exposes permissions")
    assert_contains(capabilities_service, "ProductionRolesAvailable = false", "production roles remain disabled")
    assert_contains(program, "AddSingleton<IAdminRolePermissionCatalogService, AdminRolePermissionCatalogService>()", "catalog service registration")

    for snippet in BILLING_ACTIVATION_SNIPPETS:
        assert_not_contains(capabilities_service, snippet, "production billing/Paddle or RBAC activation")

    print("Admin roles/permissions policy checks passed.")


if __name__ == "__main__":
    main()
