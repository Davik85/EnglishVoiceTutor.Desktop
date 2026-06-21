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
    "CmsRuntimeStatusReadPermissionPolicyName": ("CmsRuntimeStatusRead", "cms.runtime_status.read"),
    "CmsContentReadPermissionPolicyName": ("CmsContentRead", "cms.content.read"),
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
    {
        "action_key": "admin.users.lookup_by_email",
        "method": "GET",
        "route_constant": "AdminUserByEmailRoute",
        "permission_constant": "UserLookupRead",
        "policy_constant": "UserLookupPermissionPolicyName",
    },
    {
        "action_key": "admin.users.overview.read",
        "method": "GET",
        "route_constant": "AdminUserByIdRoute",
        "permission_constant": "UserOverviewRead",
        "policy_constant": "UserOverviewPermissionPolicyName",
    },
    {
        "action_key": "admin.users.audit.read",
        "method": "GET",
        "route_constant": "AdminUserAuditActionsRoute",
        "permission_constant": "AuditRead",
        "policy_constant": "AuditLogViewPermissionPolicyName",
    },
    {
        "action_key": "admin.premium.grant",
        "method": "POST",
        "route_constant": "AdminUserPremiumGrantsRoute",
        "permission_constant": "PremiumGrant",
        "policy_constant": "ManualPremiumGrantPermissionPolicyName",
    },
    {
        "action_key": "admin.premium.revoke",
        "method": "POST",
        "route_constant": "AdminUserPremiumGrantRevokeRoute",
        "permission_constant": "PremiumRevoke",
        "policy_constant": "ManualPremiumRevokePermissionPolicyName",
    },
    {
        "action_key": "admin.free_lesson_allowance.reset",
        "method": "POST",
        "route_constant": "AdminUserFreeLessonAllowanceResetRoute",
        "permission_constant": "FreeLessonAllowanceReset",
        "policy_constant": "FreeLessonResetPermissionPolicyName",
    },
    {
        "action_key": "admin.billing.cancel_renewal",
        "method": "POST",
        "route_constant": "AdminUserBillingCancelRenewalRoute",
        "permission_constant": "BillingCancelRenewal",
        "policy_constant": "BillingCancelRenewalPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.published_status.read",
        "method": "GET",
        "route_constant": "AdminDevCmsPublishedContentStatusRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.runtime_content_status.read",
        "method": "GET",
        "route_constant": "AdminDevCmsRuntimeContentStatusRoute",
        "permission_constant": "CmsRuntimeStatusRead",
        "policy_constant": "CmsRuntimeStatusReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.runtime_status.read",
        "method": "GET",
        "route_constant": "AdminDevCmsRuntimeStatusRoute",
        "permission_constant": "CmsRuntimeStatusRead",
        "policy_constant": "CmsRuntimeStatusReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.content_packs.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPacksRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.content_pack.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.topics.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackTopicsRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.topic.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackTopicRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.scenarios.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackScenariosRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.scenario.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackScenarioRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.prompt_templates.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackPromptTemplatesRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.prompt_template.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackPromptTemplateRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.tutor_behavior_profiles.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackTutorBehaviorProfilesRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.tutor_behavior_profile.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackTutorBehaviorProfileRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.topic.draft_save",
        "method": "PUT",
        "route_constant": "AdminDevCmsContentPackTopicRoute",
        "permission_constant": "CmsContentWriteDraft",
        "policy_constant": "CmsDraftSavePermissionPolicyName",
    },
    {
        "action_key": "admin.cms.scenario.draft_save",
        "method": "PUT",
        "route_constant": "AdminDevCmsContentPackScenarioRoute",
        "permission_constant": "CmsContentWriteDraft",
        "policy_constant": "CmsDraftSavePermissionPolicyName",
    },
    {
        "action_key": "admin.cms.prompt_template.draft_save",
        "method": "PUT",
        "route_constant": "AdminDevCmsContentPackPromptTemplateRoute",
        "permission_constant": "CmsContentWriteDraft",
        "policy_constant": "CmsDraftSavePermissionPolicyName",
    },
    {
        "action_key": "admin.cms.tutor_behavior_profile.draft_save",
        "method": "PUT",
        "route_constant": "AdminDevCmsContentPackTutorBehaviorProfileRoute",
        "permission_constant": "CmsContentWriteDraft",
        "policy_constant": "CmsDraftSavePermissionPolicyName",
    },
    {
        "action_key": "admin.cms.audit.read",
        "method": "GET",
        "route_constant": "AdminDevCmsAuditEntriesRoute",
        "permission_constant": "AuditRead",
        "policy_constant": "AuditLogViewPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.content_pack_audit.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackAuditEntriesRoute",
        "permission_constant": "AuditRead",
        "policy_constant": "AuditLogViewPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.validate",
        "method": "POST",
        "route_constant": "AdminDevCmsContentPackValidateRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.preview_summary.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackPreviewSummaryRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.versions.list",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackVersionsRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.version.read",
        "method": "GET",
        "route_constant": "AdminDevCmsContentPackVersionRoute",
        "permission_constant": "CmsContentRead",
        "policy_constant": "CmsContentReadPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.publish",
        "method": "POST",
        "route_constant": "AdminDevCmsContentPackPublishRoute",
        "permission_constant": "CmsContentPublish",
        "policy_constant": "CmsPublishPermissionPolicyName",
    },
    {
        "action_key": "admin.cms.restore",
        "method": "POST",
        "route_constant": "AdminDevCmsContentPackVersionRestoreRoute",
        "permission_constant": "CmsContentRestore",
        "policy_constant": "CmsRestorePermissionPolicyName",
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
    require(authorization_constants, 'AdminAuthorizationConfigurationSection = "AdminAuthorization"', "AdminAuthorization configuration section constant")
    require(authorization_constants, 'EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationKey = "EnableBootstrapAdminFallbackForAdminPermissionPolicies"', "BootstrapAdmin fallback cutover configuration key constant")
    require(authorization_constants, 'EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath = AdminAuthorizationConfigurationSection + ":" + EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationKey', "BootstrapAdmin fallback cutover configuration path constant")
    require(permission_handler, "IConfiguration configuration", "permission handler reads configuration")
    require(permission_handler, "AdminAuthorizationConstants.EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath", "permission handler reads named BootstrapAdmin fallback configuration")
    require(permission_handler, "GetValue<bool?>", "permission handler reads nullable fallback setting so missing config is distinguishable")
    require(permission_handler, "?? true", "missing BootstrapAdmin fallback setting defaults enabled")
    require(permission_handler, "if (!IsBootstrapAdminFallbackEnabled())", "explicit false disables BootstrapAdmin fallback")
    require(permission_handler, "return;", "fallback-disabled path fails closed without satisfying requirement")
    require(permission_handler, "_bootstrapAdminAccessService.IsBootstrapAdmin(context.User)", "permission handler reuses BootstrapAdmin access path when fallback is enabled")
    require(permission_handler, "GetBootstrapAdminPermissions()", "permission handler checks BootstrapAdmin permission catalog when fallback is enabled")
    require(program, "AddScoped<IAuthorizationHandler, AdminPermissionAuthorizationHandler>()", "permission authorization handler scoped registration for persistent role reads")
    require(program, "static void AddAdminPermissionPolicy", "central admin permission policy registration helper")

    require(permission_handler, "IAdminRoleAssignmentReadService adminRoleAssignmentReadService", "permission handler persistent role read-service dependency")
    require(permission_handler, "GetEffectiveRolesByUserIdAsync", "permission handler reads persistent roles by trusted user id claim")
    require(permission_handler, "ClaimsUserAccessor.TryGetUserId(context.User)", "permission handler uses trusted app user id claim")
    require(permission_handler, "ClaimsUserAccessor.TryGetUserEmail(context.User)", "permission handler uses trusted authenticated email fallback")
    require(permission_handler, "GetEffectiveRolesByNormalizedEmailAsync", "permission handler uses existing normalized-email read path")
    require(permission_handler, "GetProductionRolePermissions()", "permission handler checks static production role permission catalog")
    require(permission_handler, "permissions.Contains(permissionName, StringComparer.Ordinal)", "permission handler evaluates exact required permission")
    require(permission_handler, "if (await HasPersistentRolePermissionAsync(context, requirement.PermissionName))", "persistent role grant is evaluated before fallback switch")
    require(permission_handler, "context.Succeed(requirement);", "persistent role grant still succeeds regardless of fallback setting")
    require(permission_handler, "return false;", "permission handler fails closed when persistent roles do not authorize")
    require(permission_handler, "_bootstrapAdminAccessService.IsBootstrapAdmin(context.User)", "permission handler preserves BootstrapAdmin fallback")
    for forbidden_dependency in [
        "IAdminRoleAssignmentWriteService", "IAdminRoleAssignmentSafetyService", "IAdminRoleAssignmentAuditService",
        "IAdminRoleAssignmentActorResolver", "IAdminRoleAssignmentBootstrapService",
        "IAdminRoleAssignmentAdminUserProvisioningService", "AdminEndpoints", "Paddle", "Billing",
        "Subscription", "Entitlement", "Lesson", "Cms", "actorAdminUserId", "actorRoleIds",
        "[FromBody]", "Request.Query", "AdminUsers", "AdminUserRoles", "SaveChanges"
    ]:
        forbid(permission_handler, forbidden_dependency, "forbidden AdminPermissionAuthorizationHandler dependency or untrusted identity source")

    for policy_constant in DANGEROUS_POLICY_CONSTANTS:
        require(authorization_constants, policy_constant, f"explicit dangerous action policy {policy_constant}")

    require(authorization_constants, 'BootstrapAdminPolicyName = "BootstrapAdmin"', "existing BootstrapAdmin policy constant")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "non-migrated admin endpoints still use BootstrapAdmin policy")
    if "EnableBootstrapAdminFallbackForAdminPermissionPolicies" in admin_endpoints:
        raise AssertionError("BootstrapAdmin fallback cutover switch must not be applied in endpoint policy registrations.")

    endpoint_authorizations = extract_admin_endpoint_authorizations(admin_endpoints)
    permission_policy_constants = set(PRODUCTION_PERMISSION_POLICIES)
    migrated_authorizations = [
        (method.upper(), route, policy)
        for method, route, policy in endpoint_authorizations
        if policy in permission_policy_constants and route not in {"AdminRoleAssignmentDiagnosticsRoute", "AdminRoleAssignmentActorRoute", "AdminRoleAssignmentRevokeRoute", "AdminRoleAssignmentAssignRoute", "AdminRoleAssignmentBootstrapFirstOwnerRoute", "AdminRoleAssignmentDisableAdminRoute", "AdminRoleAssignmentProvisionAdminUserRoute", "AdminRoleAssignmentEnableAdminRoute", "AdminRbacCutoverStatusRoute"}
    ]
    expected_migrations = [
        (
            migrated_endpoint["method"],
            migrated_endpoint["route_constant"],
            migrated_endpoint["policy_constant"],
        )
        for migrated_endpoint in MIGRATED_ENDPOINTS
    ]
    if len(migrated_authorizations) != 35 or set(migrated_authorizations) != set(expected_migrations):
        raise AssertionError(
            f"Exactly thirty-five existing Admin endpoints must use AdminPermission:* policies after the controlled user-impacting Admin action endpoint batch migration. Got: {migrated_authorizations}"
        )

    for method, route, policy in endpoint_authorizations:
        if (method.upper(), route, policy) in expected_migrations:
            continue
        if route == "AdminRoleAssignmentDiagnosticsRoute" and method.upper() == "GET" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRbacCutoverStatusRoute" and method.upper() == "GET" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentActorRoute" and method.upper() == "GET" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentRevokeRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentAssignRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentDisableAdminRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentEnableAdminRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentBootstrapFirstOwnerRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if route == "AdminRoleAssignmentProvisionAdminUserRoute" and method.upper() == "POST" and policy == "AdminRoleManagementPermissionPolicyName":
            continue
        if policy != "BootstrapAdminPolicyName":
            raise AssertionError(f"Unexpected migrated Admin endpoint: {(method, route, policy)}")


    cms_runtime_authorization = ("GET", "AdminDevCmsRuntimeStatusRoute", "CmsRuntimeStatusReadPermissionPolicyName")
    if cms_runtime_authorization not in migrated_authorizations:
        raise AssertionError("CMS runtime status endpoint must be migrated as GET-only to CmsRuntimeStatusReadPermissionPolicyName")
    cms_content_authorization = ("GET", "AdminDevCmsContentPacksRoute", "CmsContentReadPermissionPolicyName")
    if cms_content_authorization not in migrated_authorizations:
        raise AssertionError("CMS content-packs list endpoint must be migrated as GET-only to CmsContentReadPermissionPolicyName")
    if ("GET", "AdminDevCmsContentPacksRoute", "BootstrapAdminPolicyName") in endpoint_authorizations:
        raise AssertionError("CMS content-packs list endpoint must not use BootstrapAdminPolicyName after migration")
    cms_import_init_routes = {
        "AdminDevCmsStaticContentImportRoute", "AdminDevCmsStaticJsonV1InitializeRoute",
    }
    for method, route, policy in endpoint_authorizations:
        if route in cms_import_init_routes and policy != "BootstrapAdminPolicyName":
            raise AssertionError(f"CMS import/init endpoints must remain BootstrapAdmin-protected: {(method, route, policy)}")

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
    require(catalog_service, "AdminPermissionConstants.CmsRuntimeStatusRead", "BootstrapAdmin catalog includes cms.runtime_status.read")
    require(catalog_service, "AdminPermissionConstants.CmsContentRead", "BootstrapAdmin catalog includes cms.content.read")
    require(catalog_service, "AdminPermissionConstants.UserLookupRead", "BootstrapAdmin catalog includes users.lookup.read")
    require(catalog_service, "AdminPermissionConstants.UserOverviewRead", "BootstrapAdmin catalog includes users.overview.read")
    require(catalog_service, "AdminPermissionConstants.AuditRead", "BootstrapAdmin catalog includes audit.read")

    dangerous_or_deferred_policies = set(DANGEROUS_POLICY_CONSTANTS) | {
        "LessonHistoryDiagnosticsPermissionPolicyName",
        "PremiumDiagnosticsPermissionPolicyName",
        "BillingEventDiagnosticsPermissionPolicyName",
        "SystemDiagnosticsPermissionPolicyName",
    }
    dangerous_or_deferred_policies.discard("AdminRoleManagementPermissionPolicyName")
    dangerous_or_deferred_policies.discard("ManualPremiumGrantPermissionPolicyName")
    dangerous_or_deferred_policies.discard("ManualPremiumRevokePermissionPolicyName")
    dangerous_or_deferred_policies.discard("FreeLessonResetPermissionPolicyName")
    dangerous_or_deferred_policies.discard("BillingCancelRenewalPermissionPolicyName")
    dangerous_or_deferred_policies.discard("CmsPublishPermissionPolicyName")
    dangerous_or_deferred_policies.discard("CmsRestorePermissionPolicyName")
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
