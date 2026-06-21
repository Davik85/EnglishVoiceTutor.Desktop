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
    "endpoint_catalog": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminEndpointPermissionCatalog.cs",
    "permission_handler": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminPermissionAuthorizationHandler.cs",
    "program": ROOT / "backend/EnglishVoiceTutor.Api/Program.cs",
    "admin_endpoints": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs",
    "admin_js": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js",
    "admin_index": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html",
}

PRODUCTION_PERMISSION_POLICIES = {
    "AdminSelfReadPermissionPolicyName": ("AdminSelfRead", "admin.self.read"),
    "AdminCapabilitiesReadPermissionPolicyName": ("AdminCapabilitiesRead", "admin.capabilities.read"),
    "ProductStatisticsReadPermissionPolicyName": ("ProductStatisticsRead", "product_statistics.read"),
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

MIGRATED_ENDPOINTS = [
    {
        "action_key": "admin.identity.read",
        "method": "GET",
        "route_constant": "AdminMeRoute",
        "permission_constant": "AdminSelfRead",
        "policy_constant": "AdminSelfReadPermissionPolicyName",
    },
    {
        "action_key": "admin.capabilities.read",
        "method": "GET",
        "route_constant": "AdminCapabilitiesRoute",
        "permission_constant": "AdminCapabilitiesRead",
        "policy_constant": "AdminCapabilitiesReadPermissionPolicyName",
    },
    {
        "action_key": "admin.product_overview.read",
        "method": "GET",
        "route_constant": "AdminStatisticsOverviewRoute",
        "permission_constant": "ProductStatisticsRead",
        "policy_constant": "ProductStatisticsReadPermissionPolicyName",
    },
]

DANGEROUS_ENDPOINT_MAPPINGS = {
    "admin.premium.grant": "PremiumGrant",
    "admin.premium.revoke": "PremiumRevoke",
    "admin.free_lesson_allowance.reset": "FreeLessonAllowanceReset",
    "admin.billing.cancel_renewal": "BillingCancelRenewal",
    "admin.cms.publish": "CmsContentPublish",
    "admin.cms.restore": "CmsContentRestore",
    "admin.roles.manage": "AdminRolesManage",
}

FUTURE_ONLY_ENDPOINT_PERMISSIONS = {
    "UsersRead",
    "UsersDiagnosticsRead",
    "LessonHistoryDiagnosticsRead",
    "SubscriptionsDiagnosticsRead",
    "PremiumDiagnosticsRead",
    "BillingDiagnosticsRead",
    "SystemDiagnosticsRead",
    "AdminRolesManage",
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



def extract_admin_endpoint_authorizations(admin_endpoints: str) -> list[tuple[str, str, str]]:
    return re.findall(
        r"app\.Map(Get|Post|Put|Delete)\(ApiConstants\.(Admin\w+Route),\s*[^)]*\)\s*\.RequireAuthorization\(AdminAuthorizationConstants\.(\w+)\)",
        admin_endpoints,
        flags=re.MULTILINE,
    )


def main() -> None:
    authorization_constants = read("authorization_constants")
    permission_constants = read("permission_constants")
    catalog_service = read("catalog_service")
    permission_handler = read("permission_handler")
    endpoint_catalog = read("endpoint_catalog")
    program = read("program")
    admin_endpoints = read("admin_endpoints")
    admin_ui = read("admin_js") + "\n" + read("admin_index")

    policy_values = extract_constant_values(authorization_constants, "PermissionPolicyName")
    if len(policy_values.values()) != len(set(policy_values.values())):
        raise AssertionError("Production permission policy names must be unique.")

    permission_values = extract_constant_values(permission_constants)
    if len(permission_values.values()) != len(set(permission_values.values())):
        raise AssertionError("Admin permission names must be unique.")


    require(endpoint_catalog, "public sealed record AdminEndpointPermissionMapping", "admin endpoint permission mapping record")
    require(endpoint_catalog, "public static class AdminEndpointPermissionCatalog", "static admin endpoint permission catalog")
    require(endpoint_catalog, "public static IReadOnlyList<AdminEndpointPermissionMapping> Mappings", "static admin endpoint/action mapping list")

    endpoint_mappings = re.findall(
        r'new\("([^"\n]+)",\s*"([^"\n]+)",\s*(ApiConstants\.\w+|null),\s*AdminPermissionConstants\.(\w+),\s*"([^"\n]+)"\)',
        endpoint_catalog,
    )
    if not endpoint_mappings:
        raise AssertionError("Admin endpoint/action permission catalog must contain static mappings.")

    action_keys = [mapping[0] for mapping in endpoint_mappings]
    if len(action_keys) != len(set(action_keys)):
        duplicates = sorted({key for key in action_keys if action_keys.count(key) > 1})
        raise AssertionError(f"Admin endpoint/action keys must be unique: {duplicates}")

    endpoint_permissions = {mapping[3] for mapping in endpoint_mappings}
    unknown_endpoint_permissions = endpoint_permissions - set(permission_values)
    if unknown_endpoint_permissions:
        raise AssertionError(f"Endpoint catalog maps unknown permissions: {sorted(unknown_endpoint_permissions)}")

    for action_key, permission_constant in DANGEROUS_ENDPOINT_MAPPINGS.items():
        expected = (action_key, permission_constant)
        if not any(mapping[0] == expected[0] and mapping[3] == expected[1] for mapping in endpoint_mappings):
            raise AssertionError(f"Dangerous endpoint action {action_key} must map to {permission_constant}")

    active_route_mappings = [mapping for mapping in endpoint_mappings if mapping[2] != "null"]
    active_route_constants = {mapping[2].replace("ApiConstants.", "") for mapping in active_route_mappings}
    mapped_methods_and_routes = {(mapping[1], mapping[2].replace("ApiConstants.", "")) for mapping in active_route_mappings}
    endpoint_methods_and_routes = set(re.findall(r"app\.Map(Get|Post|Put|Delete)\(ApiConstants\.(Admin\w+Route),", admin_endpoints))
    endpoint_methods_and_routes = {(method.upper(), route) for method, route in endpoint_methods_and_routes if route != "AdminSessionRoute"}
    missing_route_mappings = endpoint_methods_and_routes - mapped_methods_and_routes
    if missing_route_mappings:
        raise AssertionError(f"Active Admin endpoints missing from endpoint permission catalog: {sorted(missing_route_mappings)}")

    unknown_route_mappings = active_route_constants - set(re.findall(r"app\.Map(?:Get|Post|Put|Delete)\(ApiConstants\.(Admin\w+Route),", admin_endpoints))
    if unknown_route_mappings:
        raise AssertionError(f"Endpoint catalog references unmapped active Admin routes: {sorted(unknown_route_mappings)}")

    missing_permission_coverage = set(permission_values) - endpoint_permissions
    if missing_permission_coverage:
        raise AssertionError(f"Production permissions missing endpoint/future mapping coverage: {sorted(missing_permission_coverage)}")

    for permission_constant in FUTURE_ONLY_ENDPOINT_PERMISSIONS:
        if not any(mapping[1] == "FUTURE" and mapping[3] == permission_constant for mapping in endpoint_mappings):
            raise AssertionError(f"{permission_constant} must be deliberately documented as FUTURE in endpoint catalog")

    for policy_constant, (permission_constant, permission_name) in PRODUCTION_PERMISSION_POLICIES.items():
        require(permission_constants, f'public const string {permission_constant} = "{permission_name}"', f"permission constant for {permission_name}")
        require(authorization_constants, f'public const string {policy_constant} = "AdminPermission:{permission_name}"', f"policy constant for {permission_name}")
        require(catalog_service, f"AdminPermissionConstants.{permission_constant}", f"BootstrapAdmin catalog includes {permission_name}")
        require(program, f"AddAdminPermissionPolicy(options, AdminAuthorizationConstants.{policy_constant}, AdminPermissionConstants.{permission_constant})", f"registered permission policy mapping for {permission_name}")

    require(permission_handler, "public sealed class AdminPermissionRequirement", "AdminPermissionRequirement class")
    require(permission_handler, "public string PermissionName", "AdminPermissionRequirement permission name")
    require(permission_handler, "public sealed class AdminPermissionAuthorizationHandler", "AdminPermissionAuthorizationHandler class")
    require(permission_handler, "context.User.Identity?.IsAuthenticated != true", "permission handler authenticated-user fail closed check")
    require(permission_handler, "_bootstrapAdminAccessService.IsBootstrapAdmin(context.User)", "permission handler reuses BootstrapAdmin access path")
    require(permission_handler, "GetBootstrapAdminPermissions()", "permission handler checks BootstrapAdmin permission catalog")
    require(program, "AddSingleton<IAuthorizationHandler, AdminPermissionAuthorizationHandler>()", "permission authorization handler registration")
    require(program, "static void AddAdminPermissionPolicy", "central admin permission policy registration helper")

    for policy_constant in DANGEROUS_POLICY_CONSTANTS:
        require(authorization_constants, policy_constant, f"explicit dangerous action policy {policy_constant}")

    require(authorization_constants, 'BootstrapAdminPolicyName = "BootstrapAdmin"', "existing BootstrapAdmin policy constant")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "non-migrated admin endpoints still use BootstrapAdmin policy")

    endpoint_authorizations = extract_admin_endpoint_authorizations(admin_endpoints)
    permission_policy_constants = set(PRODUCTION_PERMISSION_POLICIES)
    migrated_authorizations = [
        (method.upper(), route, policy)
        for method, route, policy in endpoint_authorizations
        if policy in permission_policy_constants and route not in {"AdminRoleAssignmentDiagnosticsRoute", "AdminRoleAssignmentActorRoute", "AdminRoleAssignmentRevokeRoute"}
    ]
    expected_migrations = [
        (
            migrated_endpoint["method"],
            migrated_endpoint["route_constant"],
            migrated_endpoint["policy_constant"],
        )
        for migrated_endpoint in MIGRATED_ENDPOINTS
    ]
    if migrated_authorizations != expected_migrations:
        raise AssertionError(
            "Exactly three Admin endpoints must use AdminPermission:* policies, and they must be "
            f"the admin identity, capabilities, and product statistics overview endpoints. Got: {migrated_authorizations}"
        )

    for method, route, policy in endpoint_authorizations:
        if (method.upper(), route, policy) in expected_migrations:
            continue
        if route == "AdminRoleAssignmentDiagnosticsRoute" and method.upper() == "GET" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentActorRoute" and method.upper() == "GET" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentRevokeRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if policy != "BootstrapAdminPolicyName":
            raise AssertionError(f"Unexpected migrated Admin endpoint: {(method, route, policy)}")

    for migrated_endpoint in MIGRATED_ENDPOINTS:
        migrated_catalog_entries = [
            mapping for mapping in endpoint_mappings
            if mapping[0] == migrated_endpoint["action_key"]
        ]
        expected_catalog_entry = (
            migrated_endpoint["action_key"],
            migrated_endpoint["method"],
            f"ApiConstants.{migrated_endpoint['route_constant']}",
            migrated_endpoint["permission_constant"],
        )
        if len(migrated_catalog_entries) != 1 or migrated_catalog_entries[0][:4] != expected_catalog_entry:
            raise AssertionError(
                "Endpoint/action-to-permission catalog must map migrated endpoints to their "
                f"expected permissions. Got for {migrated_endpoint['action_key']}: {migrated_catalog_entries}"
            )

    require(catalog_service, "AdminPermissionConstants.AdminSelfRead", "BootstrapAdmin catalog includes admin.self.read")
    require(catalog_service, "AdminPermissionConstants.AdminCapabilitiesRead", "BootstrapAdmin catalog includes admin.capabilities.read")
    require(catalog_service, "AdminPermissionConstants.ProductStatisticsRead", "BootstrapAdmin catalog includes product_statistics.read")

    dangerous_or_deferred_policies = set(DANGEROUS_POLICY_CONSTANTS) | {
        "CmsDraftSavePermissionPolicyName",
        "UserLookupPermissionPolicyName",
        "UserOverviewPermissionPolicyName",
        "LessonHistoryDiagnosticsPermissionPolicyName",
        "PremiumDiagnosticsPermissionPolicyName",
        "BillingEventDiagnosticsPermissionPolicyName",
        "AuditLogViewPermissionPolicyName",
        "SystemDiagnosticsPermissionPolicyName",
    }
    dangerous_or_deferred_policies.discard("AdminRoleManagementPermissionPolicyName")
    for policy_constant in dangerous_or_deferred_policies:
        forbid(admin_endpoints, f"AdminAuthorizationConstants.{policy_constant}", "dangerous/write/billing/CMS/Premium/free-lesson or deferred endpoint migration")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminRoleAssignmentDiagnosticsRoute, GetAdminRoleAssignmentDiagnosticsAsync)", "new diagnostics endpoint")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "new diagnostics endpoint role-management policy")

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
