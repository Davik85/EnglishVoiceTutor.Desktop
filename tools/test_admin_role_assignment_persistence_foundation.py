#!/usr/bin/env python3
"""Static checks for the admin role assignment persistence foundation."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

APP_DB_CONTEXT = ROOT / "backend/EnglishVoiceTutor.Api/Data/AppDbContext.cs"
ENTITY_CONSTANTS = ROOT / "backend/EnglishVoiceTutor.Api/Data/EntityConstants.cs"
ENTITIES = ROOT / "backend/EnglishVoiceTutor.Api/Data/Entities"
MIGRATIONS_DIR = ROOT / "backend/EnglishVoiceTutor.Api/Migrations"
MODEL_SNAPSHOT = ROOT / "backend/EnglishVoiceTutor.Api/Migrations/AppDbContextModelSnapshot.cs"
ADMIN_HANDLER = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminPermissionAuthorizationHandler.cs"
PROGRAM = ROOT / "backend/EnglishVoiceTutor.Api/Program.cs"
ADMIN_ROLE_READ_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentReadService.cs"
ADMIN_ROLE_READ_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentReadService.cs"
ADMIN_ROLE_READ_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentReadResult.cs"
ADMIN_ROLE_ACTOR_RESOLVER = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentActorResolver.cs"
ADMIN_ROLE_ACTOR_RESOLVER_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentActorResolver.cs"
ADMIN_ROLE_ACTOR_RESOLUTION_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentActorResolutionResult.cs"
ADMIN_ROLE_SAFETY_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentSafetyService.cs"
ADMIN_ROLE_SAFETY_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentSafetyService.cs"
ADMIN_ROLE_SAFETY_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentSafetyCheckRequest.cs"
ADMIN_ROLE_SAFETY_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentSafetyCheckResult.cs"
ADMIN_ROLE_DIAGNOSTICS_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentDiagnosticsService.cs"
ADMIN_ROLE_DIAGNOSTICS_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentDiagnosticsService.cs"
ADMIN_ROLE_DIAGNOSTICS_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentDiagnosticsResult.cs"
ADMIN_ROLE_AUDIT_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAuditService.cs"
ADMIN_ROLE_AUDIT_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentAuditService.cs"
ADMIN_ROLE_AUDIT_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAuditRequest.cs"
ADMIN_ROLE_AUDIT_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAuditResult.cs"
ADMIN_ROLE_AUDIT_CONSTANTS = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAuditConstants.cs"
ADMIN_ROLE_WRITE_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentWriteService.cs"
ADMIN_ROLE_WRITE_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentWriteService.cs"
ADMIN_ROLE_WRITE_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentWriteRequest.cs"
ADMIN_ROLE_WRITE_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentWriteResult.cs"
ADMIN_ROLE_BOOTSTRAP_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentBootstrapService.cs"
ADMIN_ROLE_BOOTSTRAP_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentBootstrapService.cs"
ADMIN_ROLE_BOOTSTRAP_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentBootstrapRequest.cs"
ADMIN_ROLE_BOOTSTRAP_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentBootstrapResult.cs"
ADMIN_ROLE_PROVISIONING_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAdminUserProvisioningService.cs"
ADMIN_ROLE_PROVISIONING_INTERFACE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/IAdminRoleAssignmentAdminUserProvisioningService.cs"
ADMIN_ROLE_PROVISIONING_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAdminUserProvisioningRequest.cs"
ADMIN_ROLE_PROVISIONING_RESULT = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminRoleAssignmentAdminUserProvisioningResult.cs"
ADMIN_ROLE_REVOKE_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentRevokeRequest.cs"
ADMIN_ROLE_ASSIGN_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentAssignRequest.cs"
ADMIN_ROLE_DISABLE_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentDisableAdminRequest.cs"
ADMIN_ROLE_DISABLE_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentDisableAdminResponse.cs"
ADMIN_ROLE_ENABLE_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentEnableAdminRequest.cs"
ADMIN_ROLE_ENABLE_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentEnableAdminResponse.cs"
ADMIN_ROLE_PROVISION_HTTP_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentProvisionAdminUserRequest.cs"
ADMIN_ROLE_PROVISION_HTTP_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentProvisionAdminUserResponse.cs"
ADMIN_ROLE_BOOTSTRAP_HTTP_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentBootstrapFirstOwnerRequest.cs"
ADMIN_ROLE_BOOTSTRAP_HTTP_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentBootstrapFirstOwnerResponse.cs"
ADMIN_ROLE_ACTOR_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentActorResponse.cs"
ADMIN_ROLE_REVOKE_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentRevokeResponse.cs"
ADMIN_ROLE_ASSIGN_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminRoleAssignmentAssignResponse.cs"
API_CONSTANTS = ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs"
ADMIN_ENDPOINT_PERMISSION_CATALOG = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminEndpointPermissionCatalog.cs"
ADMIN_ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"
RELEASE_GATE = ROOT / "tools/run_desktop_release_gate.ps1"
BOOTSTRAP_SMOKE_SCRIPT = ROOT / "tools/smoke_admin_role_assignment_bootstrap_first_owner.ps1"
BOOTSTRAP_RUNBOOK = ROOT / "docs/ADMIN_ROLE_ASSIGNMENT_BOOTSTRAP_RUNBOOK.md"
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
    "UserLookupPermissionPolicyName",
    "UserOverviewPermissionPolicyName",
    "CmsRuntimeStatusReadPermissionPolicyName",
    "CmsContentReadPermissionPolicyName",
    "CmsDraftSavePermissionPolicyName",
    "CmsPublishPermissionPolicyName",
    "CmsRestorePermissionPolicyName",
    "AuditLogViewPermissionPolicyName",
    "BillingCancelRenewalPermissionPolicyName",
    "FreeLessonResetPermissionPolicyName",
    "ManualPremiumRevokePermissionPolicyName",
    "ManualPremiumGrantPermissionPolicyName",
}
BOOTSTRAP_REQUIRED_ROUTES = {
    "AdminDevCmsStaticContentImportRoute",
    "AdminDevCmsStaticJsonV1InitializeRoute",
}


def read(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def discover_admin_role_assignment_migration() -> tuple[Path, Path]:
    migrations = sorted(
        path
        for path in MIGRATIONS_DIR.glob("*_AddAdminRoleAssignmentPersistence.cs")
        if not path.name.endswith(".Designer.cs")
    )
    if len(migrations) != 1:
        found = [path.name for path in migrations]
        raise AssertionError(
            "Expected exactly one AddAdminRoleAssignmentPersistence migration file matching "
            f"*_AddAdminRoleAssignmentPersistence.cs. Found: {found}"
        )

    migration = migrations[0]
    migration_id = migration.name.removesuffix(".cs")
    migration_designer = migration.with_name(f"{migration_id}.Designer.cs")
    if not migration_designer.exists():
        raise AssertionError(
            "Missing matching AddAdminRoleAssignmentPersistence designer file: "
            f"{migration_designer.relative_to(ROOT)}"
        )

    return migration, migration_designer


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def list_source_files(root: Path) -> list[Path]:
    return [path for path in root.rglob("*") if path.is_file() and path.suffix.lower() in {".cs", ".xaml", ".js", ".html", ".ts", ".tsx", ".jsx"}]


def main() -> None:
    migration_path, migration_designer_path = discover_admin_role_assignment_migration()

    app_db_context = read(APP_DB_CONTEXT)
    entity_constants = read(ENTITY_CONSTANTS)
    migration = read(migration_path)
    migration_designer = read(migration_designer_path)
    model_snapshot = read(MODEL_SNAPSHOT)
    admin_handler = read(ADMIN_HANDLER)
    admin_endpoints = read(ADMIN_ENDPOINTS)
    program = read(PROGRAM)
    admin_role_read_service = read(ADMIN_ROLE_READ_SERVICE)
    admin_role_read_interface = read(ADMIN_ROLE_READ_INTERFACE)
    admin_role_read_result = read(ADMIN_ROLE_READ_RESULT)
    admin_role_actor_resolver = read(ADMIN_ROLE_ACTOR_RESOLVER)
    admin_role_actor_resolver_interface = read(ADMIN_ROLE_ACTOR_RESOLVER_INTERFACE)
    admin_role_actor_resolution_result = read(ADMIN_ROLE_ACTOR_RESOLUTION_RESULT)
    admin_role_safety_service = read(ADMIN_ROLE_SAFETY_SERVICE)
    admin_role_safety_interface = read(ADMIN_ROLE_SAFETY_INTERFACE)
    admin_role_safety_request = read(ADMIN_ROLE_SAFETY_REQUEST)
    admin_role_safety_result = read(ADMIN_ROLE_SAFETY_RESULT)
    admin_role_diagnostics_service = read(ADMIN_ROLE_DIAGNOSTICS_SERVICE)
    admin_role_diagnostics_interface = read(ADMIN_ROLE_DIAGNOSTICS_INTERFACE)
    admin_role_diagnostics_result = read(ADMIN_ROLE_DIAGNOSTICS_RESULT)
    admin_role_audit_service = read(ADMIN_ROLE_AUDIT_SERVICE)
    admin_role_audit_interface = read(ADMIN_ROLE_AUDIT_INTERFACE)
    admin_role_audit_request = read(ADMIN_ROLE_AUDIT_REQUEST)
    admin_role_audit_result = read(ADMIN_ROLE_AUDIT_RESULT)
    admin_role_audit_constants = read(ADMIN_ROLE_AUDIT_CONSTANTS)
    admin_role_write_service = read(ADMIN_ROLE_WRITE_SERVICE)
    admin_role_write_interface = read(ADMIN_ROLE_WRITE_INTERFACE)
    admin_role_write_request = read(ADMIN_ROLE_WRITE_REQUEST)
    admin_role_write_result = read(ADMIN_ROLE_WRITE_RESULT)
    admin_role_bootstrap_service = read(ADMIN_ROLE_BOOTSTRAP_SERVICE)
    admin_role_bootstrap_interface = read(ADMIN_ROLE_BOOTSTRAP_INTERFACE)
    admin_role_bootstrap_request = read(ADMIN_ROLE_BOOTSTRAP_REQUEST)
    admin_role_bootstrap_result = read(ADMIN_ROLE_BOOTSTRAP_RESULT)
    admin_role_provisioning_service = read(ADMIN_ROLE_PROVISIONING_SERVICE)
    admin_role_provisioning_interface = read(ADMIN_ROLE_PROVISIONING_INTERFACE)
    admin_role_provisioning_request = read(ADMIN_ROLE_PROVISIONING_REQUEST)
    admin_role_provisioning_result = read(ADMIN_ROLE_PROVISIONING_RESULT)
    admin_role_revoke_request = read(ADMIN_ROLE_REVOKE_REQUEST)
    admin_role_assign_request = read(ADMIN_ROLE_ASSIGN_REQUEST)
    admin_role_bootstrap_http_request = read(ADMIN_ROLE_BOOTSTRAP_HTTP_REQUEST)
    admin_role_bootstrap_http_response = read(ADMIN_ROLE_BOOTSTRAP_HTTP_RESPONSE)
    admin_role_actor_response = read(ADMIN_ROLE_ACTOR_RESPONSE)
    admin_role_revoke_response = read(ADMIN_ROLE_REVOKE_RESPONSE)
    admin_role_assign_response = read(ADMIN_ROLE_ASSIGN_RESPONSE)
    admin_role_disable_request = read(ADMIN_ROLE_DISABLE_REQUEST)
    admin_role_disable_response = read(ADMIN_ROLE_DISABLE_RESPONSE)
    admin_role_enable_request = read(ADMIN_ROLE_ENABLE_REQUEST)
    admin_role_enable_response = read(ADMIN_ROLE_ENABLE_RESPONSE)
    admin_role_provision_http_request = read(ADMIN_ROLE_PROVISION_HTTP_REQUEST)
    admin_role_provision_http_response = read(ADMIN_ROLE_PROVISION_HTTP_RESPONSE)
    api_constants = read(API_CONSTANTS)
    endpoint_permission_catalog = read(ADMIN_ENDPOINT_PERMISSION_CATALOG)
    release_gate = read(RELEASE_GATE)
    bootstrap_smoke_script = read(BOOTSTRAP_SMOKE_SCRIPT)
    bootstrap_runbook = read(BOOTSTRAP_RUNBOOK)




    require(admin_role_provisioning_interface, "public interface IAdminRoleAssignmentAdminUserProvisioningService", "provisioning service interface")
    require(admin_role_provisioning_interface, "ProvisionAdminUserAsync", "provisioning service method")
    require(admin_role_provisioning_service, "public sealed class AdminRoleAssignmentAdminUserProvisioningService", "provisioning service implementation")
    require(admin_role_provisioning_request, "public sealed record AdminRoleAssignmentAdminUserProvisioningRequest", "provisioning request type")
    require(admin_role_provisioning_result, "public sealed record AdminRoleAssignmentAdminUserProvisioningResult", "provisioning result type")
    require(program, "AddScoped<IAdminRoleAssignmentAdminUserProvisioningService, AdminRoleAssignmentAdminUserProvisioningService>()", "provisioning DI registration")
    for field in ["Guid ActorAdminUserId", "IReadOnlyList<string> ActorRoleIds", "Guid TargetAppUserId", "string? TargetNormalizedEmail", "string Reason", "string? SafeMetadataJson"]:
        require(admin_role_provisioning_request, field, f"provisioning request field {field}")
    forbidden_request_patterns = {
        "RoleId": r"(?<!Actor)RoleId\b",
        "TargetRole": r"TargetRole\b",
        "ActorEmail": r"ActorEmail\b",
        "Claims": r"Claims\b",
        "Password": r"Password\b",
        "Token": r"Token\b",
        "Invite": r"Invite\b",
    }
    for forbidden, pattern in forbidden_request_patterns.items():
        if re.search(pattern, admin_role_provisioning_request):
            raise AssertionError(f"Provisioning request must not accept unsafe or role-assignment field: {forbidden}")
    for field in ["bool IsSuccess", "string? ErrorCode", "string? Message", "Guid? AdminUserId", "Guid? AuditEventId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_provisioning_result, field, f"provisioning result field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "MetadataJson", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_provisioning_result:
            raise AssertionError(f"Provisioning result must not expose unsafe field: {forbidden}")
    for forbidden in ["IAdminRoleAssignmentAdminUserProvisioningService", "AdminRoleAssignmentAdminUserProvisioningService", "AdminUserProvisioning"]:
        if forbidden in admin_handler:
            raise AssertionError(f"AdminPermissionAuthorizationHandler must not reference provisioning or persistent role services: {forbidden}")
    for route_term in ["create-admin", "invite"]:
        if route_term in api_constants.lower() or route_term in admin_endpoints.lower():
            raise AssertionError(f"Create-admin/invite/enable-admin HTTP routes must not be added: {route_term}")
    for required in [
        "request.ActorAdminUserId == Guid.Empty",
        "request.ActorRoleIds is null || request.ActorRoleIds.Count == 0",
        "CanProvisionAdminUsers(request.ActorRoleIds)",
        "AdminRoleConstants.SuperAdmin",
        "OwnerRoleId",
        "request.TargetAppUserId == Guid.Empty",
        "normalizedReason is null",
        "AnyAsync(user => user.Id == request.TargetAppUserId",
        "adminUser.UserId == request.TargetAppUserId",
        "activeExistingTarget is not null",
        "inactiveExistingTarget is not null",
        "adminUser.NormalizedEmail == normalizedEmail",
        "adminUser.UserId != request.TargetAppUserId",
        "BeginTransactionAsync",
        "IAdminRoleAssignmentAuditService",
        "AppendAuditEventAsync",
        "AdminRoleAssignmentAuditConstants.ActionTypes.AdminUserProvisioned",
        "AdminRoleAssignmentAuditConstants.ActionTypes.AdminUserProvisioningDenied",
        "_dbContext.AdminUsers.AddAsync",
    ]:
        require(admin_role_provisioning_service, required, f"provisioning safety/audit/write behavior {required}")
    for forbidden in ["AdminUserRoles.Add", "AdminUserRoles.AddAsync", "new AdminUserRoleEntity", "AssignRoleAsync", "RevokeRoleAsync", "DisableAdminAsync", "EnableAdmin", "Invite", "Subscriptions", "Entitlements", "Billing", "Payments", "Paddle", "Lessons", "Cms", "Desktop"]:
        if forbidden in admin_role_provisioning_service:
            raise AssertionError(f"Provisioning service must not write role assignments or unrelated domains: {forbidden}")


    if not BOOTSTRAP_SMOKE_SCRIPT.exists():
        raise AssertionError("Bootstrap first-owner smoke script must still exist.")
    require(bootstrap_smoke_script, "ConfirmCreateFirstOwner", "explicit first-owner bootstrap smoke confirmation flag")
    require(bootstrap_smoke_script, "AllowProductionUrl", "explicit production/non-local URL override flag")
    require(bootstrap_smoke_script, "http://localhost:5000", "local-only default BaseUrl convention")
    require(bootstrap_smoke_script, "$BootstrapFirstOwnerPath = \"/api/admin/role-assignments/bootstrap-first-owner\"", "bootstrap smoke endpoint path")
    require(bootstrap_smoke_script, "$ActorPath = \"/api/admin/role-assignments/actor\"", "actor smoke endpoint path")
    require(bootstrap_smoke_script, "$DiagnosticsPath = \"/api/admin/role-assignments/diagnostics\"", "diagnostics smoke endpoint path")
    require(bootstrap_smoke_script, "if (-not $ConfirmCreateFirstOwner)", "confirmation guard before HTTP calls")

    require(bootstrap_smoke_script, '$HealthPath = "/health"', "harmless backend health endpoint preflight path")
    require(bootstrap_smoke_script, "Test-BackendReachability", "backend reachability preflight helper")
    require(bootstrap_smoke_script, "Preflight backend reachability without creating data", "preflight step before login/bootstrap flow")
    require(bootstrap_smoke_script, "Invoke-WebRequest -Method $MethodGet -Uri $healthUrl", "safe GET reachability check")
    require(bootstrap_smoke_script, "DefaultConnection", "DefaultConnection prerequisite message")
    require(bootstrap_smoke_script, "The normal desktop tester flow does not require running a local backend", "desktop tester flow prerequisite message")
    preflight_index = bootstrap_smoke_script.index("Test-BackendReachability -TargetBaseUrl $BaseUrl")
    login_step_index = bootstrap_smoke_script.index('Write-Step "Login using the existing local admin smoke-test pattern"')
    bootstrap_step_index = bootstrap_smoke_script.index('Write-Step "POST first-owner bootstrap with server-side authenticated identity"')
    if not (preflight_index < login_step_index < bootstrap_step_index):
        raise AssertionError("Bootstrap smoke preflight must run before login and before the mutating bootstrap flow.")
    first_http_call_index = min(index for index in [bootstrap_smoke_script.find("Invoke-RestMethod"), bootstrap_smoke_script.find("Invoke-WebRequest")] if index != -1)
    confirmation_guard_index = bootstrap_smoke_script.index("if (-not $ConfirmCreateFirstOwner)")
    if confirmation_guard_index > first_http_call_index:
        raise AssertionError("Bootstrap smoke confirmation guard must appear before any HTTP call helper invocation.")
    if "smoke_admin_role_assignment_bootstrap_first_owner.ps1" in release_gate:
        raise AssertionError("First-owner bootstrap smoke script must not be referenced by the desktop release gate.")
    if re.search(r"https://api\.languagevoicetutor\.com\s*['\"]?\s*(?:,|\)|$)", bootstrap_smoke_script):
        raise AssertionError("First-owner bootstrap smoke script must not target production by default.")
    for unsafe_literal in ["PADDLE_API_KEY", "PADDLE_WEBHOOK_SECRET", "BEGIN PRIVATE KEY", "eyJhbGci", "sk-", "password123", "admin@", "owner@", "client_secret", "access_token="]:
        if unsafe_literal.lower() in bootstrap_smoke_script.lower() or unsafe_literal.lower() in bootstrap_runbook.lower():
            raise AssertionError(f"Bootstrap smoke/runbook must not contain secrets, tokens, or real admin email-like values: {unsafe_literal}")
    bootstrap_body_match = re.search(r"\$bootstrapBody\s*=\s*@\{(?P<body>[\s\S]*?)\n\}", bootstrap_smoke_script)
    if not bootstrap_body_match:
        raise AssertionError("Bootstrap smoke script must build an explicit bootstrap request body hashtable.")
    bootstrap_body = bootstrap_body_match.group("body")
    for required_body_field in ["reason = $Reason", "safeMetadataJson = $SafeMetadataJson"]:
        require(bootstrap_body, required_body_field, f"bootstrap smoke request body field {required_body_field}")
    bootstrap_body_keys = re.findall(r"^\s*([A-Za-z][A-Za-z0-9_]*)\s*=", bootstrap_body, flags=re.MULTILINE)
    if set(bootstrap_body_keys) != {"reason", "safeMetadataJson"}:
        raise AssertionError(f"Bootstrap smoke request body must send only reason and safeMetadataJson. Found: {bootstrap_body_keys}")
    for forbidden_body_field in ["appUserId", "normalizedEmail", "email", "targetAdminUserId", "actorAdminUserId", "actorRoleIds", "roleId"]:
        if re.search(rf"(^|[^A-Za-z]){forbidden_body_field}\s*=", bootstrap_body, flags=re.IGNORECASE):
            raise AssertionError(f"Bootstrap smoke request body must not send {forbidden_body_field}.")
    for runbook_phrase in [
        "controlled manual validation operation only",
        "creates only the first persistent Owner/SuperAdmin-equivalent mapping",
        "server-side authenticated claims",
        "Persistent roles are still not active in global authorization",
        "AdminPermissionAuthorizationHandler uses persistent read service only for AdminPermission:* policy evaluation and still avoids safety, audit, write, actor, bootstrap, and provisioning services",
        "Admin UI role management still does not exist",
        "database/audit-aware operations",
        "This runbook is not part of the normal desktop tester flow",
        "The target backend must already be running",
        "DefaultConnection` must be configured outside committed repository files",
        "If the local backend/database is intentionally not configured, do not run this smoke",
        "Do not commit local database connection strings, secrets, credentials, tokens, or real admin emails",
        "known safe local or controlled test environment",
        "may create the first persistent `AdminUser` and Owner/SuperAdmin-equivalent role mapping",
        "This error means the local backend was started without the required local database configuration",
        "does not mean the desktop app, the controlled tester release backend, or the Windows release package is broken",
    ]:
        require(bootstrap_runbook, runbook_phrase, f"bootstrap runbook phrase: {runbook_phrase}")

    migration_id = migration_path.name.removesuffix(".cs")
    require(migration_designer, f'[Migration("{migration_id}")]', "matching EF migration metadata id")
    require(migration_designer, "[DbContext(typeof(AppDbContext))]", "migration DbContext metadata")
    require(migration_designer, "void BuildTargetModel(ModelBuilder modelBuilder)", "EF migration target model")

    for entity_name in ["AdminUserEntity", "AdminUserRoleEntity", "AdminRoleAssignmentEventEntity"]:
        entity_text = read(ENTITIES / f"{entity_name}.cs")
        require(entity_text, f"public sealed class {entity_name}", f"{entity_name} declaration")
        require(app_db_context, f"DbSet<{entity_name}>", f"{entity_name} DbSet")
        require(migration_designer, f'Entity("EnglishVoiceTutor.Api.Data.Entities.{entity_name}"', f"{entity_name} migration metadata")
        require(model_snapshot, f'Entity("EnglishVoiceTutor.Api.Data.Entities.{entity_name}"', f"{entity_name} model snapshot")

    for constant_name, table_name in [
        ("AdminUsers", "admin_users"),
        ("AdminUserRoles", "admin_user_roles"),
        ("AdminRoleAssignmentEvents", "admin_role_assignment_events"),
    ]:
        require(entity_constants, f'public const string {constant_name} = "{table_name}";', f"{table_name} table constant")
        require(app_db_context, f"EntityConstants.TableNames.{constant_name}", f"{table_name} mapping")
        require(migration, f'name: "{table_name}"', f"{table_name} migration table")
        require(migration_designer, f'.ToTable("{table_name}"', f"{table_name} migration metadata table")
        require(model_snapshot, f'.ToTable("{table_name}"', f"{table_name} model snapshot table")

    created_tables = set(re.findall(r'CreateTable\(\s*\n\s*name: "([^"]+)"', migration))
    if created_tables != EXPECTED_TABLES:
        raise AssertionError(f"Migration must create only admin role assignment persistence tables. Found: {sorted(created_tables)}")

    for label, ef_text in [("migration", migration), ("migration metadata", migration_designer), ("model snapshot", model_snapshot)]:
        if re.search(r'InsertData\(|UpdateData\(|DeleteData\(', ef_text):
            raise AssertionError(f"{label} must not seed, update, or delete data.")
        if re.search(r'[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}', ef_text):
            raise AssertionError(f"{label} must not contain real admin emails or users.")
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


    require(admin_role_read_interface, "public interface IAdminRoleAssignmentReadService", "read service interface")
    require(admin_role_read_interface, "GetEffectiveRolesByUserIdAsync", "user-id role read method")
    require(admin_role_read_interface, "GetEffectiveRolesByNormalizedEmailAsync", "normalized-email role read method")
    require(admin_role_read_result, "public sealed record AdminRoleAssignmentReadResult", "read result record")
    require(admin_role_read_result, "IReadOnlyList<string> RoleIds", "read result role ids")
    require(admin_role_read_service, "public sealed class AdminRoleAssignmentReadService", "read service implementation")
    require(admin_role_read_service, "AppDbContext dbContext", "read service AppDbContext dependency")
    require(admin_role_read_service, "IAdminRolePermissionCatalogService", "known production role catalog dependency")
    require(admin_role_read_service, "_dbContext.AdminUsers", "read service reads AdminUsers")
    require(admin_role_read_service, "_dbContext.AdminUserRoles", "read service reads AdminUserRoles")
    require(admin_role_read_service, ".AsNoTracking()", "read-only no-tracking queries")
    require(admin_role_read_service, "adminUser.DisabledAtUtc.HasValue", "disabled admin filter")
    require(admin_role_read_service, "!string.Equals(adminUser.Status, ActiveStatus", "inactive admin status filter")
    require(admin_role_read_service, "role.RevokedAtUtc == null", "revoked role filter")
    require(admin_role_read_service, "knownRoleIds.Contains(role.RoleId)", "unknown persistent role filter")
    require(program, "AddScoped<IAdminRoleAssignmentReadService, AdminRoleAssignmentReadService>()", "read service DI registration")





    require(admin_role_actor_resolver_interface, "public interface IAdminRoleAssignmentActorResolver", "actor resolver interface")
    require(admin_role_actor_resolver_interface, "ResolveActorAsync", "actor resolver method")
    require(admin_role_actor_resolver_interface, "ClaimsPrincipal principal", "actor resolver trusted principal input")
    require(admin_role_actor_resolution_result, "public sealed record AdminRoleAssignmentActorResolutionResult", "actor resolution result record")
    for field in ["Guid? ActorAdminUserId", "IReadOnlyList<string> ActorRoleIds", "bool IsActorMappingFound", "string? ErrorCode", "string? Message"]:
        require(admin_role_actor_resolution_result, field, f"actor resolution result field {field}")
    require(admin_role_actor_resolver, "public sealed class AdminRoleAssignmentActorResolver", "actor resolver implementation")
    require(admin_role_actor_resolver, "IAdminRoleAssignmentReadService adminRoleAssignmentReadService", "actor resolver read service dependency")
    require(admin_role_actor_resolver, "ClaimsUserAccessor.TryGetUserId(principal)", "actor resolver reads trusted user id claim")
    require(admin_role_actor_resolver, "ClaimsUserAccessor.TryGetUserEmail(principal)", "actor resolver reads trusted email claim")
    require(admin_role_actor_resolver, "GetEffectiveRolesByUserIdAsync", "actor resolver uses user-id read path")
    require(admin_role_actor_resolver, "GetEffectiveRolesByNormalizedEmailAsync", "actor resolver uses normalized-email read path")
    require(admin_role_actor_resolver, "admin_role_assignment_actor_mapping_unavailable", "actor resolver stable fail-closed error code")
    require(program, "AddScoped<IAdminRoleAssignmentActorResolver, AdminRoleAssignmentActorResolver>()", "actor resolver DI registration")
    for forbidden in ["AdminRoleAssignmentRevokeRequest", "ActorAdminUserId", "ActorRoleIds", "[FromBody]", "Request.Body", ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".Remove(", "SaveChanges", "ExecuteUpdate", "ExecuteDelete"]:
        if forbidden in admin_role_actor_resolver:
            raise AssertionError(f"AdminRoleAssignmentActorResolver must use trusted server-side principal/read service only and stay read-only: {forbidden}")

    require(admin_role_safety_interface, "public interface IAdminRoleAssignmentSafetyService", "safety service interface")
    require(admin_role_safety_interface, "ValidateAssignRoleAsync", "assign-role safety method")
    require(admin_role_safety_interface, "ValidateRevokeRoleAsync", "revoke-role safety method")
    require(admin_role_safety_interface, "ValidateDisableAdminAsync", "disable-admin safety method")
    require(admin_role_safety_interface, "ValidateEnableAdminAsync", "enable-admin safety method")
    require(admin_role_safety_request, "public sealed record AdminRoleAssignmentSafetyCheckRequest", "safety request record")
    require(admin_role_safety_request, "Guid ActorAdminUserId", "safety request actor admin id")
    require(admin_role_safety_request, "Guid TargetAdminUserId", "safety request target admin id")
    require(admin_role_safety_request, "IReadOnlyList<string> ActorRoleIds", "safety request actor roles")
    require(admin_role_safety_request, "string? Reason", "safety request reason")
    require(admin_role_safety_result, "public sealed record AdminRoleAssignmentSafetyCheckResult", "safety result record")
    require(admin_role_safety_result, "bool IsAllowed", "safety result allow flag")
    require(admin_role_safety_result, "string? ErrorCode", "safety result error code")
    require(admin_role_safety_result, "IReadOnlyList<string> Violations", "safety result violations")
    require(admin_role_safety_service, "public sealed class AdminRoleAssignmentSafetyService", "safety service implementation")
    require(admin_role_safety_service, "AppDbContext dbContext", "safety service AppDbContext dependency")
    require(admin_role_safety_service, "IAdminRolePermissionCatalogService", "safety service production role catalog dependency")
    require(admin_role_safety_service, "GetProductionRolePermissions()", "safety validates known roles from production catalog")
    require(admin_role_safety_service, "A non-empty human-readable reason is required.", "safety validates reason requirement")
    require(admin_role_safety_service, "Only Owner or SuperAdmin actors may manage Admin roles.", "safety validates owner/super-admin-only management")
    require(admin_role_safety_service, "Only Owner or SuperAdmin actors may grant elevated Admin roles.", "safety validates no self-escalation for elevated roles")
    require(admin_role_safety_service, "Cannot revoke SuperAdmin from the last active SuperAdmin.", "safety validates last super-admin revoke protection")
    require(admin_role_safety_service, "Cannot disable the last active SuperAdmin.", "safety validates last super-admin disable protection")
    require(admin_role_safety_service, "Cannot assign a role to a disabled admin user.", "safety validates disabled target protection")
    require(admin_role_safety_service, "Target admin user does not exist.", "safety validates unknown target protection")
    for required in [
        "ValidateEnableAdminAsync",
        "request.ActorAdminUserId == Guid.Empty",
        "request.ActorRoleIds is null || request.ActorRoleIds.Count == 0",
        "Only Owner or SuperAdmin actors may manage Admin roles.",
        "request.TargetAdminUserId == Guid.Empty",
        "A non-empty human-readable reason is required.",
        "Target admin user does not exist.",
        "Target admin user is already active.",
        "Target admin user must have a linked app user id or normalized email before it can be enabled.",
        "admin_role_assignment_enable_denied",
    ]:
        require(admin_role_safety_service, required, f"enable-admin safety check {required}")
    require(admin_role_safety_service, "_dbContext.AdminUsers", "safety reads AdminUsers")
    require(admin_role_safety_service, "_dbContext.AdminUserRoles", "safety reads AdminUserRoles")
    require(admin_role_safety_service, ".AsNoTracking()", "safety no-tracking reads")
    require(program, "AddScoped<IAdminRoleAssignmentSafetyService, AdminRoleAssignmentSafetyService>()", "safety service DI registration")
    for forbidden in ["SaveChanges", "SaveChangesAsync", ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".UpdateRange(", ".Remove(", ".RemoveRange(", "ExecuteUpdate", "ExecuteDelete", "AdminRoleAssignmentEvents"]:
        if forbidden in admin_role_safety_service:
            raise AssertionError(f"AdminRoleAssignmentSafetyService must stay validation-only/read-only and must not use: {forbidden}")


    require(admin_role_audit_interface, "public interface IAdminRoleAssignmentAuditService", "audit service interface")
    require(admin_role_audit_interface, "AppendAuditEventAsync", "audit append method")
    require(admin_role_audit_request, "public sealed record AdminRoleAssignmentAuditRequest", "audit request record")
    for field in [
        "Guid? ActorAdminUserId",
        "Guid TargetAdminUserId",
        "string ActionType",
        "string? RoleId",
        "string? Reason",
        "IReadOnlyList<string>? OldRoles",
        "IReadOnlyList<string>? NewRoles",
        "string Result",
        "string? SafeMetadataJson",
    ]:
        require(admin_role_audit_request, field, f"audit request field {field}")
    require(admin_role_audit_result, "public sealed record AdminRoleAssignmentAuditResult", "audit result record")
    require(admin_role_audit_result, "Guid EventId", "audit result event id")
    require(admin_role_audit_result, "DateTimeOffset OccurredAtUtc", "audit result occurred timestamp")
    require(admin_role_audit_constants, "public static class AdminRoleAssignmentAuditConstants", "audit constants class")
    for constant_value in [
        "assign_role", "revoke_role", "disable_admin", "enable_admin", "invite_created",
        "invite_revoked", "last_owner_blocked", "self_escalation_blocked", "validation_denied",
        "succeeded", "denied", "failed_validation", "failed_conflict",
    ]:
        require(admin_role_audit_constants, f'"{constant_value}"', f"audit constant {constant_value}")
    require(admin_role_audit_service, "public sealed class AdminRoleAssignmentAuditService", "audit service implementation")
    require(admin_role_audit_service, "AppDbContext dbContext", "audit service AppDbContext dependency")
    require(admin_role_audit_service, "IAdminRolePermissionCatalogService", "audit service production role catalog dependency")
    require(admin_role_audit_service, "new AdminRoleAssignmentEventEntity", "audit service writes event entity only")
    require(admin_role_audit_service, "_dbContext.AdminRoleAssignmentEvents.AddAsync", "audit service appends only role assignment events")
    require(admin_role_audit_service, "_dbContext.SaveChangesAsync(cancellationToken)", "audit service saves appended audit event")
    require(program, "AddScoped<IAdminRoleAssignmentAuditService, AdminRoleAssignmentAuditService>()", "audit service DI registration")
    for required_validation in [
        "Target admin user id must not be empty.",
        "ActionType must not be empty.",
        "Result must not be empty.",
        "A non-empty human-readable reason is required for safety-sensitive Admin role assignment audit events.",
        "Role id is not a known production Admin role.",
        "Safe metadata JSON is too long for Admin role assignment audit storage.",
        "must not contain secret, credential, or raw provider payload fields",
    ]:
        require(admin_role_audit_service, required_validation, f"audit validation: {required_validation}")
    for forbidden in [
        "_dbContext.AdminUsers", "_dbContext.AdminUserRoles", "new AdminUserEntity", "new AdminUserRoleEntity",
        "Subscriptions", "Entitlements", "Lessons", "Cms", "Paddle", "Billing", "Payment",
        "PasswordReset", "UserRefreshToken", "TokenHash", "PasswordHash",
    ]:
        if forbidden in admin_role_audit_service:
            raise AssertionError(f"AdminRoleAssignmentAuditService must not write/read unrelated tables or secret-bearing fields: {forbidden}")
    for forbidden in [".Update(", ".UpdateRange(", ".Remove(", ".RemoveRange(", "ExecuteUpdate", "ExecuteDelete", ".Attach("]:
        if forbidden in admin_role_audit_service:
            raise AssertionError(f"AdminRoleAssignmentAuditService must append audit events only and must not use: {forbidden}")


    require(admin_role_write_interface, "public interface IAdminRoleAssignmentWriteService", "write service interface")
    for method in ["AssignRoleAsync", "RevokeRoleAsync", "DisableAdminAsync", "EnableAdminAsync"]:
        require(admin_role_write_interface, method, f"write interface method {method}")
        require(admin_role_write_service, method, f"write service method {method}")
    require(admin_role_write_request, "public sealed record AdminRoleAssignmentWriteRequest", "write request record")
    for field in [
        "Guid ActorAdminUserId",
        "Guid TargetAdminUserId",
        "string? RoleId",
        "IReadOnlyList<string> ActorRoleIds",
        "string? Reason",
        "string? SafeMetadataJson",
    ]:
        require(admin_role_write_request, field, f"write request field {field}")
    require(admin_role_write_result, "public sealed record AdminRoleAssignmentWriteResult", "write result record")
    for field in [
        "bool IsSuccess",
        "string? ErrorCode",
        "string? Message",
        "Guid? AuditEventId",
        "Guid TargetAdminUserId",
        "string? RoleId",
        "DateTimeOffset OccurredAtUtc",
    ]:
        require(admin_role_write_result, field, f"write result field {field}")
    require(admin_role_write_service, "public sealed class AdminRoleAssignmentWriteService", "write service implementation")
    require(admin_role_write_service, "AppDbContext dbContext", "write service AppDbContext dependency")
    require(admin_role_write_service, "IAdminRoleAssignmentSafetyService safetyService", "write service safety dependency")
    require(admin_role_write_service, "IAdminRoleAssignmentAuditService auditService", "write service audit dependency")
    require(admin_role_write_service, "ValidateAssignRoleAsync", "write service validates before assign mutation")
    require(admin_role_write_service, "ValidateRevokeRoleAsync", "write service validates before revoke mutation")
    require(admin_role_write_service, "ValidateDisableAdminAsync", "write service validates before disable mutation")
    require(admin_role_write_service, "ValidateEnableAdminAsync", "write service validates before enable mutation")
    require(admin_role_write_service, "AppendAuditEventAsync", "write service appends audit events through audit service")
    require(admin_role_write_service, "BeginTransactionAsync", "write service uses EF transactions")
    require(admin_role_write_service, "CommitAsync", "write service commits successful operations")
    require(admin_role_write_service, "new AdminUserRoleEntity", "write service creates role assignments")
    require(admin_role_write_service, "_dbContext.AdminUserRoles.AddAsync", "write service adds role assignment rows")
    require(admin_role_write_service, "RevokedAtUtc = occurredAtUtc", "write service revokes without deleting role rows")
    require(admin_role_write_service, "RevokedByAdminUserId = request.ActorAdminUserId", "write service records revoker")
    require(admin_role_write_service, "RevokeReason = request.Reason!.Trim()", "write service records revoke reason")
    require(admin_role_write_service, "target.Status = \"disabled\"", "write service disables admin using status")
    require(admin_role_write_service, "target.DisabledAtUtc = occurredAtUtc", "write service records disabled timestamp")
    require(admin_role_write_service, "AdminRoleAssignmentAuditConstants.ActionTypes.EnableAdmin", "write service audits enable-admin action")
    require(admin_role_write_service, "target.Status = ActiveStatus", "write service re-enables admin using active status")
    require(admin_role_write_service, "target.DisabledAtUtc = null", "write service clears disabled timestamp")
    require(admin_role_write_service, "AdminRoleAssignmentAuditConstants.Results.Succeeded", "write service audits successes")
    require(admin_role_write_service, "AdminRoleAssignmentAuditConstants.Results.FailedValidation", "write service audits safety denials")
    require(admin_role_write_service, "AdminRoleAssignmentAuditConstants.Results.FailedConflict", "write service audits conflicts")
    require(program, "AddScoped<IAdminRoleAssignmentWriteService, AdminRoleAssignmentWriteService>()", "write service DI registration")
    if admin_role_write_service.index("ValidateAssignRoleAsync") > admin_role_write_service.index("new AdminUserRoleEntity"):
        raise AssertionError("AssignRoleAsync must call safety validation before creating AdminUserRoleEntity.")
    if admin_role_write_service.index("ValidateRevokeRoleAsync") > admin_role_write_service.index("RevokedAtUtc = occurredAtUtc"):
        raise AssertionError("RevokeRoleAsync must call safety validation before revoking roles.")
    if admin_role_write_service.index("ValidateDisableAdminAsync") > admin_role_write_service.index("target.Status = \"disabled\""):
        raise AssertionError("DisableAdminAsync must call safety validation before disabling admins.")
    if admin_role_write_service.index("ValidateEnableAdminAsync") > admin_role_write_service.index("target.Status = ActiveStatus"):
        raise AssertionError("EnableAdminAsync must call safety validation before enabling admins.")
    enable_method = admin_role_write_service[
        admin_role_write_service.index("public async Task<AdminRoleAssignmentWriteResult> EnableAdminAsync"):
        admin_role_write_service.index("private static AdminRoleAssignmentSafetyCheckRequest ToSafetyRequest")
    ]
    for required in [
        "request.ActorAdminUserId",
        "request.TargetAdminUserId",
        "request.Reason",
        "BeginTransactionAsync",
        "AppendAuditEventAsync",
        "AdminRoleAssignmentAuditConstants.ActionTypes.EnableAdmin",
        "AdminRoleAssignmentAuditConstants.Results.Succeeded",
    ]:
        require(enable_method, required, f"enable-admin write method requirement {required}")
    for forbidden in [
        "new AdminUserEntity", "new AdminUserRoleEntity", "AdminUserRoles.Add", "AdminUserRoles.AddAsync",
        "AssignRoleAsync", "RevokeRoleAsync", "DisableAdminAsync", "Subscriptions", "Entitlements",
        "Lessons", "Cms", "Paddle", "Billing", "Payment", "Desktop",
    ]:
        if forbidden in enable_method:
            raise AssertionError(f"EnableAdminAsync must not assign/revoke roles, create users, or touch unrelated state: {forbidden}")
    for forbidden in [
        "new AdminUserEntity", "CreateAdminUser", "Invite", "Remove(", "RemoveRange(", "ExecuteDelete",
        "Subscriptions", "Entitlements", "Lessons", "Cms", "Paddle", "Billing", "Payment",
        "Desktop", "PasswordReset", "UserRefreshToken", "TokenHash", "PasswordHash",
    ]:
        if forbidden in admin_role_write_service:
            raise AssertionError(f"AdminRoleAssignmentWriteService must not create users/invites, delete rows, or touch unrelated state: {forbidden}")



    require(admin_role_bootstrap_interface, "public interface IAdminRoleAssignmentBootstrapService", "bootstrap service interface")
    require(admin_role_bootstrap_interface, "BootstrapFirstOwnerAsync", "bootstrap first owner method")
    require(admin_role_bootstrap_request, "public sealed record AdminRoleAssignmentBootstrapRequest", "bootstrap request record")
    for field in ["Guid AppUserId", "string? NormalizedEmail", "string ActorReason", "string? SafeMetadataJson"]:
        require(admin_role_bootstrap_request, field, f"bootstrap request trusted field {field}")
    for forbidden in ["TargetAdminUserId", "TargetUserId", "AdminUserId", "RoleId", "ActorRoleIds", "AssignedByAdminUserId", "Email"]:
        if forbidden in admin_role_bootstrap_request.replace("NormalizedEmail", ""):
            raise AssertionError(f"Bootstrap request must not accept arbitrary target/admin/role/actor-role fields: {forbidden}")
    require(admin_role_bootstrap_result, "public sealed record AdminRoleAssignmentBootstrapResult", "bootstrap result record")
    for field in ["bool IsSuccess", "string? ErrorCode", "string? Message", "Guid? AdminUserId", "string? RoleId", "Guid? AuditEventId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_bootstrap_result, field, f"bootstrap result field {field}")
    require(admin_role_bootstrap_service, "public sealed class AdminRoleAssignmentBootstrapService", "bootstrap service implementation")
    require(admin_role_bootstrap_service, "AppDbContext dbContext", "bootstrap service AppDbContext dependency")
    require(admin_role_bootstrap_service, "IAdminRoleAssignmentAuditService auditService", "bootstrap service audit dependency")
    require(admin_role_bootstrap_service, "InitialOwnerRoleId = AdminRoleConstants.SuperAdmin", "bootstrap uses SuperAdmin owner-equivalent role")
    require(admin_role_bootstrap_service, "request.AppUserId == Guid.Empty", "bootstrap requires app user id")
    require(admin_role_bootstrap_service, "string.IsNullOrWhiteSpace(request.ActorReason)", "bootstrap requires reason")
    require(admin_role_bootstrap_service, "role.RoleId == InitialOwnerRoleId && role.RevokedAtUtc == null", "bootstrap checks existing active owner role")
    require(admin_role_bootstrap_service, "role.AdminUser.Status == ActiveStatus && role.AdminUser.DisabledAtUtc == null", "bootstrap checks owner admin is active non-disabled")
    require(admin_role_bootstrap_service, "adminUser.UserId == request.AppUserId", "bootstrap checks same app-user mappings")
    require(admin_role_bootstrap_service, "adminUser.DisabledAtUtc.HasValue", "bootstrap rejects disabled mappings")
    require(admin_role_bootstrap_service, "adminUser.RoleAssignments.Any(role => role.RevokedAtUtc == null)", "bootstrap rejects active mapping with active roles")
    require(admin_role_bootstrap_service, "adminUser.NormalizedEmail == normalizedEmail", "bootstrap checks normalized email conflict")
    require(admin_role_bootstrap_service, "adminUser.UserId != request.AppUserId", "bootstrap rejects email mapped to different active admin")
    require(admin_role_bootstrap_service, "BeginTransactionAsync", "bootstrap uses EF transaction")
    require(admin_role_bootstrap_service, "CommitAsync", "bootstrap commits successful transaction")
    require(admin_role_bootstrap_service, "new AdminUserEntity", "bootstrap creates AdminUserEntity")
    require(admin_role_bootstrap_service, "new AdminUserRoleEntity", "bootstrap creates AdminUserRoleEntity")
    require(admin_role_bootstrap_service, "_dbContext.AdminUsers.AddAsync", "bootstrap writes AdminUsers")
    require(admin_role_bootstrap_service, "_dbContext.AdminUserRoles.AddAsync", "bootstrap writes AdminUserRoles")
    require(admin_role_bootstrap_service, "AppendAuditEventAsync", "bootstrap uses audit service")
    require(admin_role_bootstrap_service, "AdminRoleAssignmentAuditConstants.ActionTypes.FirstOwnerBootstrap", "bootstrap audits success action")
    require(admin_role_bootstrap_service, "AdminRoleAssignmentAuditConstants.ActionTypes.ValidationDenied", "bootstrap audits denials when target exists")
    require(admin_role_bootstrap_service, "AdminRoleAssignmentAuditConstants.Results.Succeeded", "bootstrap success audit result")
    require(admin_role_bootstrap_service, "AdminRoleAssignmentAuditConstants.Results.FailedValidation", "bootstrap denied audit result")
    require(admin_role_audit_constants, 'FirstOwnerBootstrap = "first_owner_bootstrap"', "bootstrap audit action constant")
    require(admin_role_audit_service, "AdminRoleAssignmentAuditConstants.ActionTypes.FirstOwnerBootstrap", "audit service accepts bootstrap action")
    require(program, "AddScoped<IAdminRoleAssignmentBootstrapService, AdminRoleAssignmentBootstrapService>()", "bootstrap service DI registration")
    for forbidden in ["_dbContext.Users", "Subscriptions", "Entitlements", "Lessons", "Cms", "Paddle", "Billing", "Payment", "Desktop", "PasswordReset", "UserRefreshToken", "TokenHash", "PasswordHash", "Invite"]:
        if forbidden in admin_role_bootstrap_service:
            raise AssertionError(f"Bootstrap service must not touch unrelated state or invite flows: {forbidden}")
    require(admin_role_diagnostics_interface, "public interface IAdminRoleAssignmentDiagnosticsService", "diagnostics service interface")
    require(admin_role_diagnostics_interface, "GetDiagnosticsAsync", "diagnostics read method")
    require(admin_role_diagnostics_result, "public sealed record AdminRoleAssignmentDiagnosticsResult", "diagnostics result record")
    for field in [
        "int TotalAdminUsers",
        "int ActiveAdminUsers",
        "int DisabledAdminUsers",
        "int PendingInviteAdminUsers",
        "int TotalRoleAssignments",
        "int ActiveRoleAssignments",
        "int RevokedRoleAssignments",
        "int TotalRoleAssignmentEvents",
        "IReadOnlyList<string> RolesInUse",
        "DateTimeOffset GeneratedAtUtc",
    ]:
        require(admin_role_diagnostics_result, field, f"diagnostics result field {field}")
    require(admin_role_diagnostics_result, "public sealed record AdminRoleAssignmentDiagnosticsUserResult", "safe per-admin diagnostics result")
    require(admin_role_diagnostics_result, "Guid? LinkedUserId", "nullable linked user id only")
    if "Email" in admin_role_diagnostics_result or "NormalizedEmail" in admin_role_diagnostics_result:
        raise AssertionError("Diagnostics response must not expose email fields in this first endpoint.")
    require(admin_role_diagnostics_service, "public sealed class AdminRoleAssignmentDiagnosticsService", "diagnostics service implementation")
    require(admin_role_diagnostics_service, "_dbContext.AdminUsers", "diagnostics reads AdminUsers")
    require(admin_role_diagnostics_service, "_dbContext.AdminUserRoles", "diagnostics reads AdminUserRoles")
    require(admin_role_diagnostics_service, "_dbContext.AdminRoleAssignmentEvents", "diagnostics reads AdminRoleAssignmentEvents")
    require(admin_role_diagnostics_service, ".AsNoTracking()", "diagnostics no-tracking reads")
    require(admin_role_diagnostics_service, "CountAsync(cancellationToken)", "diagnostics event aggregate count")
    require(program, "AddScoped<IAdminRoleAssignmentDiagnosticsService, AdminRoleAssignmentDiagnosticsService>()", "diagnostics service DI registration")
    require(api_constants, 'AdminRoleAssignmentDiagnosticsRoute = "/api/admin/role-assignments/diagnostics"', "diagnostics route constant")
    require(api_constants, 'AdminRoleAssignmentActorRoute = "/api/admin/role-assignments/actor"', "actor mapping route constant")
    require(api_constants, 'AdminRoleAssignmentRevokeRoute = "/api/admin/role-assignments/revoke"', "revoke route constant")
    require(api_constants, 'AdminRoleAssignmentAssignRoute = "/api/admin/role-assignments/assign"', "assign route constant")
    require(api_constants, 'AdminRoleAssignmentDisableAdminRoute = "/api/admin/role-assignments/disable-admin"', "disable-admin route constant")
    require(api_constants, 'AdminRoleAssignmentEnableAdminRoute = "/api/admin/role-assignments/enable-admin"', "enable-admin route constant")
    require(api_constants, 'AdminRoleAssignmentProvisionAdminUserRoute = "/api/admin/role-assignments/provision-admin-user"', "provision-admin-user route constant")
    require(api_constants, 'AdminRoleAssignmentBootstrapFirstOwnerRoute = "/api/admin/role-assignments/bootstrap-first-owner"', "bootstrap first owner route constant")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminRoleAssignmentDiagnosticsRoute, GetAdminRoleAssignmentDiagnosticsAsync)", "GET-only diagnostics endpoint")
    require(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "diagnostics role-management permission policy")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.diagnostics.read", "GET", ApiConstants.AdminRoleAssignmentDiagnosticsRoute, AdminPermissionConstants.AdminRolesManage', "diagnostics endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.actor.read", "GET", ApiConstants.AdminRoleAssignmentActorRoute, AdminPermissionConstants.AdminRolesManage', "actor endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.revoke", "POST", ApiConstants.AdminRoleAssignmentRevokeRoute, AdminPermissionConstants.AdminRolesManage', "revoke endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.assign", "POST", ApiConstants.AdminRoleAssignmentAssignRoute, AdminPermissionConstants.AdminRolesManage', "assign endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.disable_admin", "POST", ApiConstants.AdminRoleAssignmentDisableAdminRoute, AdminPermissionConstants.AdminRolesManage', "disable-admin endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.enable_admin", "POST", ApiConstants.AdminRoleAssignmentEnableAdminRoute, AdminPermissionConstants.AdminRolesManage', "enable-admin endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.provision_admin_user", "POST", ApiConstants.AdminRoleAssignmentProvisionAdminUserRoute, AdminPermissionConstants.AdminRolesManage', "provision-admin-user endpoint permission catalog mapping")
    require(endpoint_permission_catalog, 'new("admin.role_assignments.bootstrap_first_owner", "POST", ApiConstants.AdminRoleAssignmentBootstrapFirstOwnerRoute, AdminPermissionConstants.AdminRolesManage', "bootstrap endpoint permission catalog mapping")
    if re.search(r"AdminRoleAssignmentDiagnosticsRoute[\s\S]{0,220}BootstrapAdminPolicyName", admin_endpoints):
        raise AssertionError("Diagnostics endpoint must not use BootstrapAdminPolicyName directly.")
    if re.search(r"Map(Post|Put|Delete)\(ApiConstants\.AdminRoleAssignmentDiagnosticsRoute", admin_endpoints):
        raise AssertionError("Diagnostics endpoint must be GET-only.")
    require(admin_endpoints, "app.MapGet(ApiConstants.AdminRoleAssignmentActorRoute, GetAdminRoleAssignmentActorAsync)", "GET-only actor mapping endpoint")
    actor_registration_start = admin_endpoints.index("app.MapGet(ApiConstants.AdminRoleAssignmentActorRoute, GetAdminRoleAssignmentActorAsync)")
    actor_registration_end = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentRevokeRoute", actor_registration_start)
    actor_registration = admin_endpoints[actor_registration_start:actor_registration_end]
    require(actor_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "actor endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in actor_registration:
        raise AssertionError("Actor mapping endpoint must not use BootstrapAdminPolicyName directly.")
    if re.search(r"Map(Post|Put|Delete)\(ApiConstants\.AdminRoleAssignmentActorRoute", admin_endpoints):
        raise AssertionError("Actor mapping endpoint must be GET-only.")
    actor_handler_start = admin_endpoints.index("private static async Task<IResult> GetAdminRoleAssignmentActorAsync")
    actor_handler_end = admin_endpoints.index("private static async Task<IResult> BootstrapFirstOwnerAdminRoleAssignmentAsync")
    actor_handler = admin_endpoints[actor_handler_start:actor_handler_end]
    require(actor_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "actor endpoint actor resolver dependency")
    require(actor_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "actor endpoint calls actor resolver")
    require(actor_handler, "new AdminRoleAssignmentActorResponse", "actor endpoint returns response contract")
    for forbidden in ["IAdminRoleAssignmentWriteService", "IAdminRoleAssignmentAuditService", "_dbContext", ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".Remove(", "SaveChanges", "ExecuteUpdate", "ExecuteDelete", "[FromBody]"]:
        if forbidden in actor_handler:
            raise AssertionError(f"Actor mapping endpoint must stay read-only and must not use: {forbidden}")
    require(admin_role_actor_response, "public sealed class AdminRoleAssignmentActorResponse", "actor response contract")
    for field in ["bool IsActorMappingFound", "Guid? ActorAdminUserId", "IReadOnlyList<string> RoleIds", "string? ErrorCode", "string? Message", "DateTimeOffset GeneratedAtUtc"]:
        require(admin_role_actor_response, field, f"safe actor response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "Metadata", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_actor_response:
            raise AssertionError(f"Actor response must not expose unsafe field: {forbidden}")
    for forbidden in [".Add(", ".AddAsync(", ".Attach(", ".Update(", ".UpdateRange(", ".Remove(", ".RemoveRange(", "SaveChanges", "ExecuteUpdate", "ExecuteDelete"]:
        if forbidden in admin_role_diagnostics_service:
            raise AssertionError(f"AdminRoleAssignmentDiagnosticsService must stay read-only and must not use: {forbidden}")
    for forbidden in ["AssignedByAdminUserId =", "RevokedByAdminUserId =", "CreatedByAdminUserId =", "CreateInvite", "InviteToken", "AssignRole", "CreateAdminUser"]:
        if forbidden in admin_role_diagnostics_service:
            raise AssertionError(f"Diagnostics endpoint must not create or mutate role assignments/admin users/invites: {forbidden}")

    forbidden_write_terms = [
        ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".UpdateRange(", ".Remove(", ".RemoveRange(",
        "SaveChanges", "ExecuteUpdate", "ExecuteDelete", "AdminRoleAssignmentEvents"
    ]
    for forbidden in forbidden_write_terms:
        if forbidden in admin_role_read_service:
            raise AssertionError(f"AdminRoleAssignmentReadService must stay read-only and must not use: {forbidden}")

    require(admin_handler, "IAdminRoleAssignmentReadService adminRoleAssignmentReadService", "AdminPermissionAuthorizationHandler persistent read-service dependency")
    require(admin_handler, "IAdminRolePermissionCatalogService adminRolePermissionCatalogService", "AdminPermissionAuthorizationHandler role permission catalog dependency")
    require(admin_handler, "ClaimsUserAccessor.TryGetUserId(context.User)", "AdminPermissionAuthorizationHandler trusted user id claim identity")
    require(admin_handler, "ClaimsUserAccessor.TryGetUserEmail(context.User)", "AdminPermissionAuthorizationHandler trusted email claim fallback")
    require(admin_handler, "GetEffectiveRolesByUserIdAsync", "AdminPermissionAuthorizationHandler persistent role lookup by user id")
    require(admin_handler, "GetEffectiveRolesByNormalizedEmailAsync", "AdminPermissionAuthorizationHandler existing normalized-email lookup path")
    require(admin_handler, "GetProductionRolePermissions()", "AdminPermissionAuthorizationHandler static role permission catalog lookup")
    require(admin_handler, "permissions.Contains(permissionName, StringComparer.Ordinal)", "AdminPermissionAuthorizationHandler exact permission check")
    require(admin_handler, "_bootstrapAdminAccessService.IsBootstrapAdmin(context.User)", "AdminPermissionAuthorizationHandler BootstrapAdmin fallback")
    require(admin_handler, "GetBootstrapAdminPermissions()", "AdminPermissionAuthorizationHandler BootstrapAdmin fallback permission catalog")
    require(admin_handler, "return false;", "AdminPermissionAuthorizationHandler persistent authorization fail-closed path")
    for forbidden in ["IAdminRoleAssignmentActorResolver", "AdminRoleAssignmentActorResolver", "IAdminRoleAssignmentSafetyService", "AdminRoleAssignmentSafetyService", "IAdminRoleAssignmentAuditService", "AdminRoleAssignmentAuditService", "IAdminRoleAssignmentWriteService", "AdminRoleAssignmentWriteService", "IAdminRoleAssignmentBootstrapService", "AdminRoleAssignmentBootstrapService", "AdminRoleAssignmentBootstrap", "IAdminRoleAssignmentAdminUserProvisioningService", "AdminRoleAssignmentAdminUserProvisioningService", "EnableAdminAsync", "ValidateEnableAdminAsync", "AdminUsers", "AdminUserRoles", "AdminRoleAssignmentEvents", "admin_users", "admin_user_roles", "admin_role_assignment_events", "[FromBody]", "Request.Query", "actorAdminUserId", "actorRoleIds", "Paddle", "Billing", "Subscription", "Entitlement", "Lesson", "Cms"]:
        if forbidden in admin_handler:
            raise AssertionError(f"AdminPermissionAuthorizationHandler must not use forbidden dependency, table, endpoint, or untrusted identity source: {forbidden}")

    endpoint_authorizations = re.findall(
        r"app\.Map(Get|Post|Put|Delete)\(ApiConstants\.(Admin\w+Route),\s*[^)]*\)\s*\.RequireAuthorization\(AdminAuthorizationConstants\.(\w+)\)",
        admin_endpoints,
        flags=re.MULTILINE,
    )
    permission_migrated = {(method.upper(), route, policy) for method, route, policy in endpoint_authorizations if policy.endswith("PermissionPolicyName") and route not in {"AdminRoleAssignmentDiagnosticsRoute", "AdminRoleAssignmentActorRoute", "AdminRoleAssignmentRevokeRoute", "AdminRoleAssignmentAssignRoute", "AdminRoleAssignmentDisableAdminRoute", "AdminRoleAssignmentEnableAdminRoute", "AdminRoleAssignmentProvisionAdminUserRoute", "AdminRoleAssignmentBootstrapFirstOwnerRoute"}}
    if {policy for _, _, policy in permission_migrated} != MIGRATED_POLICY_CONSTANTS or len(permission_migrated) != 35:
        raise AssertionError(f"Exactly thirty-five existing Admin endpoints must remain permission-policy migrated after user-impacting Admin action endpoint migration. Found: {sorted(permission_migrated)}")
    if ("GET", "AdminDevCmsRuntimeStatusRoute", "CmsRuntimeStatusReadPermissionPolicyName") not in permission_migrated:
        raise AssertionError("CMS runtime status must remain a GET-only AdminPermission migration.")
    if ("GET", "AdminDevCmsContentPacksRoute", "CmsContentReadPermissionPolicyName") not in permission_migrated:
        raise AssertionError("CMS content-packs list must remain a GET-only AdminPermission migration.")
    for expected_user_read_migration in [
        ("GET", "AdminUserByEmailRoute", "UserLookupPermissionPolicyName"),
        ("GET", "AdminUserByIdRoute", "UserOverviewPermissionPolicyName"),
        ("GET", "AdminUserAuditActionsRoute", "AuditLogViewPermissionPolicyName"),
    ]:
        if expected_user_read_migration not in permission_migrated:
            raise AssertionError(f"User lookup/overview/audit endpoint must remain a GET-only narrow AdminPermission migration: {expected_user_read_migration}")

    route_to_policy = {route: policy for _, route, policy in endpoint_authorizations}
    diagnostics_endpoint_count = len(re.findall(r"MapGet\(ApiConstants\.AdminRoleAssignmentDiagnosticsRoute", admin_endpoints))
    if diagnostics_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one read-only role assignment diagnostics endpoint. Found: {diagnostics_endpoint_count}")
    actor_endpoint_count = len(re.findall(r"MapGet\(ApiConstants\.AdminRoleAssignmentActorRoute", admin_endpoints))
    if actor_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one read-only role assignment actor mapping endpoint. Found: {actor_endpoint_count}")

    role_assignment_write_endpoints = re.findall(r"Map(Post|Put|Delete)\(ApiConstants\.(AdminRoleAssignment\w+Route)", admin_endpoints)
    if role_assignment_write_endpoints != [("Post", "AdminRoleAssignmentRevokeRoute"), ("Post", "AdminRoleAssignmentAssignRoute"), ("Post", "AdminRoleAssignmentDisableAdminRoute"), ("Post", "AdminRoleAssignmentEnableAdminRoute"), ("Post", "AdminRoleAssignmentProvisionAdminUserRoute"), ("Post", "AdminRoleAssignmentBootstrapFirstOwnerRoute")]:
        raise AssertionError(f"Expected exactly six role assignment write endpoints: POST revoke, POST assign, POST disable-admin, POST enable-admin, POST provision-admin-user, and POST bootstrap-first-owner. Found: {role_assignment_write_endpoints}")
    assign_endpoint_count = len(re.findall(r"MapPost\(ApiConstants\.AdminRoleAssignmentAssignRoute", admin_endpoints))
    if assign_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one assign role assignment endpoint. Found: {assign_endpoint_count}")
    if re.search(r"Map(Get|Put|Delete)\(ApiConstants\.AdminRoleAssignmentAssignRoute", admin_endpoints):
        raise AssertionError("Assign role assignment endpoint must be POST-only.")
    disable_endpoint_count = len(re.findall(r"MapPost\(ApiConstants\.AdminRoleAssignmentDisableAdminRoute", admin_endpoints))
    if disable_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one disable-admin endpoint. Found: {disable_endpoint_count}")
    if re.search(r"Map(Get|Put|Delete)\(ApiConstants\.AdminRoleAssignmentDisableAdminRoute", admin_endpoints):
        raise AssertionError("Disable-admin endpoint must be POST-only.")
    bootstrap_endpoint_count = len(re.findall(r"MapPost\(ApiConstants\.AdminRoleAssignmentBootstrapFirstOwnerRoute", admin_endpoints))
    if bootstrap_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one bootstrap-first-owner endpoint. Found: {bootstrap_endpoint_count}")
    if re.search(r"Map(Get|Put|Delete)\(ApiConstants\.AdminRoleAssignmentBootstrapFirstOwnerRoute", admin_endpoints):
        raise AssertionError("Bootstrap first owner endpoint must be POST-only.")
    revoke_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentRevokeRoute, RevokeAdminRoleAssignmentAsync)")
    next_map_index = admin_endpoints.find("app.Map", revoke_map_index + 1)
    revoke_registration = admin_endpoints[revoke_map_index:next_map_index]
    require(revoke_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "revoke endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in revoke_registration:
        raise AssertionError("Revoke endpoint must not use BootstrapAdminPolicyName directly.")

    assign_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentAssignRoute, AssignAdminRoleAssignmentAsync)")
    assign_next_map_index = admin_endpoints.find("app.Map", assign_map_index + 1)
    assign_registration = admin_endpoints[assign_map_index:assign_next_map_index]
    require(assign_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "assign endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in assign_registration:
        raise AssertionError("Assign endpoint must not use BootstrapAdminPolicyName directly.")

    disable_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentDisableAdminRoute, DisableAdminRoleAssignmentAsync)")
    disable_next_map_index = admin_endpoints.find("app.Map", disable_map_index + 1)
    disable_registration = admin_endpoints[disable_map_index:disable_next_map_index]
    require(disable_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "disable-admin endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in disable_registration:
        raise AssertionError("Disable-admin endpoint must not use BootstrapAdminPolicyName directly.")

    enable_endpoint_count = len(re.findall(r"MapPost\(ApiConstants\.AdminRoleAssignmentEnableAdminRoute", admin_endpoints))
    if enable_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one enable-admin endpoint. Found: {enable_endpoint_count}")
    if re.search(r"Map(Get|Put|Delete)\(ApiConstants\.AdminRoleAssignmentEnableAdminRoute", admin_endpoints):
        raise AssertionError("Enable-admin endpoint must be POST-only.")
    enable_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentEnableAdminRoute, EnableAdminRoleAssignmentAsync)")
    enable_next_map_index = admin_endpoints.find("app.Map", enable_map_index + 1)
    enable_registration = admin_endpoints[enable_map_index:enable_next_map_index]
    require(enable_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "enable-admin endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in enable_registration:
        raise AssertionError("Enable-admin endpoint must not use BootstrapAdminPolicyName directly.")

    provision_endpoint_count = len(re.findall(r"MapPost\(ApiConstants\.AdminRoleAssignmentProvisionAdminUserRoute", admin_endpoints))
    if provision_endpoint_count != 1:
        raise AssertionError(f"Expected exactly one provision-admin-user endpoint. Found: {provision_endpoint_count}")
    if re.search(r"Map(Get|Put|Delete)\(ApiConstants\.AdminRoleAssignmentProvisionAdminUserRoute", admin_endpoints):
        raise AssertionError("Provision-admin-user endpoint must be POST-only.")
    provision_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentProvisionAdminUserRoute, ProvisionAdminUserRoleAssignmentAsync)")
    provision_next_map_index = admin_endpoints.find("app.Map", provision_map_index + 1)
    provision_registration = admin_endpoints[provision_map_index:provision_next_map_index]
    require(provision_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "provision-admin-user endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in provision_registration:
        raise AssertionError("Provision-admin-user endpoint must not use BootstrapAdminPolicyName directly.")

    bootstrap_map_index = admin_endpoints.index("app.MapPost(ApiConstants.AdminRoleAssignmentBootstrapFirstOwnerRoute, BootstrapFirstOwnerAdminRoleAssignmentAsync)")
    bootstrap_next_map_index = admin_endpoints.find("app.Map", bootstrap_map_index + 1)
    bootstrap_registration = admin_endpoints[bootstrap_map_index:bootstrap_next_map_index]
    require(bootstrap_registration, "RequireAuthorization(AdminAuthorizationConstants.AdminRoleManagementPermissionPolicyName)", "bootstrap endpoint role-management permission policy")
    if "BootstrapAdminPolicyName" in bootstrap_registration:
        raise AssertionError("Bootstrap endpoint must not use BootstrapAdminPolicyName directly.")

    bootstrap_handler_start = admin_endpoints.index("private static async Task<IResult> BootstrapFirstOwnerAdminRoleAssignmentAsync")
    bootstrap_handler_end = admin_endpoints.index("private static async Task<IResult> RevokeAdminRoleAssignmentAsync")
    bootstrap_handler = admin_endpoints[bootstrap_handler_start:bootstrap_handler_end]
    require(bootstrap_handler, "IAdminRoleAssignmentBootstrapService adminRoleAssignmentBootstrapService", "bootstrap endpoint service dependency")
    require(bootstrap_handler, "ClaimsUserAccessor.TryGetUserId(principal)", "bootstrap endpoint derives app user id from trusted principal")
    require(bootstrap_handler, "ClaimsUserAccessor.TryGetUserEmail(principal)", "bootstrap endpoint derives email from trusted principal")
    require(bootstrap_handler, "BootstrapFirstOwnerAsync(new AdminRoleAssignmentBootstrapRequest", "bootstrap endpoint calls bootstrap service")
    require(bootstrap_handler, "Results.Unauthorized()", "bootstrap endpoint fails closed when app user id is unavailable")
    require(bootstrap_handler, "Results.Ok(response) : Results.Conflict(response)", "bootstrap endpoint maps success and safe failure responses")
    require(admin_role_bootstrap_http_request, "public sealed class AdminRoleAssignmentBootstrapFirstOwnerRequest", "bootstrap http request contract")
    for field in ["string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_bootstrap_http_request, field, f"bootstrap http request field {field}")
    for forbidden in ["AppUserId", "NormalizedEmail", "Email", "TargetAdminUserId", "ActorAdminUserId", "ActorRoleIds", "RoleId"]:
        if forbidden in admin_role_bootstrap_http_request:
            raise AssertionError(f"Bootstrap HTTP request must not accept trusted/server-owned field: {forbidden}")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AdminUserId", "string? RoleId", "Guid? AuditEventId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_bootstrap_http_response, field, f"safe bootstrap response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "Metadata", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_bootstrap_http_response:
            raise AssertionError(f"Bootstrap response must not expose unsafe field: {forbidden}")
    for forbidden in ["IAdminRoleAssignmentWriteService", "IAdminRoleAssignmentAuditService", "_dbContext", "new AdminUser", "new AdminUserRole", ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".Remove(", "SaveChanges", "ExecuteUpdate", "ExecuteDelete", "AdminRoleAssignmentWriteService", "AdminRoleAssignmentAuditService"]:
        if forbidden in bootstrap_handler:
            raise AssertionError(f"Bootstrap endpoint must delegate only to bootstrap service and must not use: {forbidden}")

    assign_handler_start = admin_endpoints.index("private static async Task<IResult> AssignAdminRoleAssignmentAsync")
    assign_handler_end = admin_endpoints.index("private static async Task<IResult> DisableAdminRoleAssignmentAsync")
    assign_handler = admin_endpoints[assign_handler_start:assign_handler_end]
    require(assign_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "assign endpoint actor resolver dependency")
    require(assign_handler, "IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService", "assign endpoint write service dependency")
    require(assign_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "assign endpoint calls actor resolver")
    require(assign_handler, "AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode", "assign endpoint stable fail-closed actor mapping error")
    require(assign_handler, "actorResolution.ActorAdminUserId.Value", "assign endpoint passes resolver actor id to write service")
    require(assign_handler, "actorResolution.ActorRoleIds", "assign endpoint passes resolver actor roles to write service")
    require(assign_handler, "adminRoleAssignmentWriteService.AssignRoleAsync", "assign endpoint calls write service assign")
    require(assign_handler, "Results.Ok(response) : Results.Conflict(response)", "assign endpoint maps success and safe failure responses")
    require(admin_role_assign_request, "public sealed class AdminRoleAssignmentAssignRequest", "assign endpoint request contract")
    for field in ["Guid TargetAdminUserId", "string? RoleId", "string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_assign_request, field, f"assign request field {field}")
    for forbidden in ["AppUserId", "NormalizedEmail", "Email", "ActorAdminUserId", "ActorRoleIds", "AssignedByAdminUserId", "CreatedBy", "TargetEmail"]:
        if forbidden in admin_role_assign_request:
            raise AssertionError(f"Assign request must not accept trusted/client-supplied or email field: {forbidden}.")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AuditEventId", "Guid TargetAdminUserId", "string? RoleId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_assign_response, field, f"safe assign response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "Metadata", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_assign_response:
            raise AssertionError(f"Assign response must not expose unsafe field: {forbidden}")
    for forbidden in ["RevokeRoleAsync", "DisableAdminAsync", "_dbContext", "AdminUserRoles", "new AdminUser", "new AdminUserRole", "SaveChanges", "AppendAuditEventAsync", "AdminRoleAssignmentAuditService", "ActorAdminUserId =", "ActorRoleIds ="]:
        if forbidden in assign_handler:
            raise AssertionError(f"Assign endpoint must not revoke/disable, mutate EF directly, audit directly, or trust actor fields: {forbidden}")

    revoke_handler_start = admin_endpoints.index("private static async Task<IResult> RevokeAdminRoleAssignmentAsync")
    revoke_handler_end = admin_endpoints.index("private static async Task<IResult> AssignAdminRoleAssignmentAsync")
    revoke_handler = admin_endpoints[revoke_handler_start:revoke_handler_end]
    require(revoke_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "revoke endpoint actor resolver dependency")
    require(revoke_handler, "IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService", "revoke endpoint write service dependency")
    require(revoke_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "revoke endpoint calls actor resolver")
    require(revoke_handler, "AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode", "revoke endpoint stable fail-closed actor mapping error")
    require(revoke_handler, "actorResolution.ActorAdminUserId.Value", "revoke endpoint passes resolver actor id to write service")
    require(revoke_handler, "actorResolution.ActorRoleIds", "revoke endpoint passes resolver actor roles to write service")
    require(revoke_handler, "adminRoleAssignmentWriteService.RevokeRoleAsync", "revoke endpoint calls write service revoke")
    require(admin_role_revoke_request, "public sealed class AdminRoleAssignmentRevokeRequest", "revoke endpoint request contract")
    for field in ["Guid TargetAdminUserId", "string? RoleId", "string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_revoke_request, field, f"revoke request field {field}")
    for forbidden in ["ActorAdminUserId", "ActorRoleIds"]:
        if forbidden in admin_role_revoke_request:
            raise AssertionError(f"Revoke request must not accept trusted client-supplied {forbidden}.")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AuditEventId", "Guid TargetAdminUserId", "string? RoleId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_revoke_response, field, f"safe revoke response field {field}")
    for forbidden in ["AssignRoleAsync", "DisableAdminAsync", "_dbContext", "AdminUserRoles", "RevokedAtUtc =", "SaveChanges", "AppendAuditEventAsync", "ActorAdminUserId =", "ActorRoleIds ="]:
        if forbidden in revoke_handler:
            raise AssertionError(f"Revoke endpoint must not assign/disable, mutate EF directly, audit directly, or trust actor fields: {forbidden}")

    disable_handler_start = admin_endpoints.index("private static async Task<IResult> DisableAdminRoleAssignmentAsync")
    disable_handler_end = admin_endpoints.index("private static async Task<IResult> EnableAdminRoleAssignmentAsync")
    disable_handler = admin_endpoints[disable_handler_start:disable_handler_end]
    require(disable_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "disable-admin endpoint actor resolver dependency")
    require(disable_handler, "IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService", "disable-admin endpoint write service dependency")
    require(disable_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "disable-admin endpoint calls actor resolver")
    require(disable_handler, "AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode", "disable-admin endpoint stable fail-closed actor mapping error")
    require(disable_handler, "actorResolution.ActorAdminUserId.Value", "disable-admin endpoint passes resolver actor id to write service")
    require(disable_handler, "actorResolution.ActorRoleIds", "disable-admin endpoint passes resolver actor roles to write service")
    require(disable_handler, "adminRoleAssignmentWriteService.DisableAdminAsync", "disable-admin endpoint calls write service disable")
    require(disable_handler, "Results.Ok(response) : Results.Conflict(response)", "disable-admin endpoint maps success and safe failure responses")
    require(admin_role_disable_request, "public sealed class AdminRoleAssignmentDisableAdminRequest", "disable-admin request contract")
    for field in ["Guid TargetAdminUserId", "string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_disable_request, field, f"disable-admin request field {field}")
    for forbidden in ["AppUserId", "NormalizedEmail", "Email", "ActorAdminUserId", "ActorRoleIds", "DisabledByAdminUserId", "CreatedBy", "TargetEmail", "RoleId"]:
        if forbidden in admin_role_disable_request:
            raise AssertionError(f"Disable-admin request must not accept trusted/client-supplied, email, or role field: {forbidden}.")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AuditEventId", "Guid TargetAdminUserId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_disable_response, field, f"safe disable-admin response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "Metadata", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_disable_response:
            raise AssertionError(f"Disable-admin response must not expose unsafe field: {forbidden}")
    for forbidden in ["AssignRoleAsync", "RevokeRoleAsync", "_dbContext", "AdminUsers", "AdminUserRoles", "DisabledAtUtc =", "Status =", "SaveChanges", "AppendAuditEventAsync", "AdminRoleAssignmentAuditService", "ActorAdminUserId =", "ActorRoleIds ="]:
        if forbidden in disable_handler:
            raise AssertionError(f"Disable-admin endpoint must not assign/revoke, mutate EF directly, audit directly, or trust actor fields: {forbidden}")


    enable_handler_start = admin_endpoints.index("private static async Task<IResult> EnableAdminRoleAssignmentAsync")
    enable_handler_end = admin_endpoints.index("private static async Task<IResult> ProvisionAdminUserRoleAssignmentAsync")
    enable_handler = admin_endpoints[enable_handler_start:enable_handler_end]
    require(enable_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "enable-admin endpoint actor resolver dependency")
    require(enable_handler, "IAdminRoleAssignmentWriteService adminRoleAssignmentWriteService", "enable-admin endpoint write service dependency")
    require(enable_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "enable-admin endpoint calls actor resolver")
    require(enable_handler, "AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode", "enable-admin endpoint stable fail-closed actor mapping error")
    require(enable_handler, "actorResolution.ActorAdminUserId.Value", "enable-admin endpoint passes resolver actor id to write service")
    require(enable_handler, "actorResolution.ActorRoleIds", "enable-admin endpoint passes resolver actor roles to write service")
    require(enable_handler, "adminRoleAssignmentWriteService.EnableAdminAsync", "enable-admin endpoint calls write service enable")
    require(enable_handler, "Results.Ok(response) : Results.Conflict(response)", "enable-admin endpoint maps success and safe failure responses")
    require(admin_role_enable_request, "public sealed class AdminRoleAssignmentEnableAdminRequest", "enable-admin request contract")
    for field in ["Guid TargetAdminUserId", "string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_enable_request, field, f"enable-admin request field {field}")
    for forbidden in ["AppUserId", "NormalizedEmail", "Email", "TargetEmail", "ActorAdminUserId", "ActorRoleIds", "RoleId", "Password", "Token", "InviteToken", "CreatedBy", "AssignedByAdminUserId", "DisabledByAdminUserId", "EnabledByAdminUserId"]:
        if forbidden in admin_role_enable_request:
            raise AssertionError(f"Enable-admin request must not accept forbidden field: {forbidden}.")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AuditEventId", "Guid TargetAdminUserId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_enable_response, field, f"safe enable-admin response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "MetadataJson", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_enable_response:
            raise AssertionError(f"Enable-admin response must not expose unsafe field: {forbidden}")
    for forbidden in ["AssignRoleAsync", "RevokeRoleAsync", "DisableAdminAsync", "IAdminRoleAssignmentAdminUserProvisioningService", "IAdminRoleAssignmentBootstrapService", "_dbContext", "AdminUsers", "AdminUserRoles", "DisabledAtUtc =", "Status =", "SaveChanges", "AppendAuditEventAsync", "AdminRoleAssignmentAuditService", "ActorAdminUserId =", "ActorRoleIds ="]:
        if forbidden in enable_handler:
            raise AssertionError(f"Enable-admin endpoint must not assign/revoke/disable, provision/bootstrap, mutate EF directly, audit directly, or trust actor fields: {forbidden}")

    provision_handler_start = admin_endpoints.index("private static async Task<IResult> ProvisionAdminUserRoleAssignmentAsync")
    provision_handler_end = admin_endpoints.index("private static async Task<IResult> GetAdminUserByEmailAsync")
    provision_handler = admin_endpoints[provision_handler_start:provision_handler_end]
    require(provision_handler, "IAdminRoleAssignmentActorResolver adminRoleAssignmentActorResolver", "provision-admin-user endpoint actor resolver dependency")
    require(provision_handler, "IAdminRoleAssignmentAdminUserProvisioningService adminRoleAssignmentAdminUserProvisioningService", "provision-admin-user endpoint provisioning service dependency")
    require(provision_handler, "adminRoleAssignmentActorResolver.ResolveActorAsync(principal, cancellationToken)", "provision-admin-user endpoint calls actor resolver")
    require(provision_handler, "AdminRoleAssignmentActorResolver.ActorMappingUnavailableErrorCode", "provision-admin-user endpoint stable fail-closed actor mapping error")
    require(provision_handler, "actorResolution.ActorAdminUserId.Value", "provision-admin-user endpoint passes resolver actor id")
    require(provision_handler, "actorResolution.ActorRoleIds", "provision-admin-user endpoint passes resolver actor roles")
    require(provision_handler, "adminRoleAssignmentAdminUserProvisioningService.ProvisionAdminUserAsync", "provision-admin-user endpoint calls provisioning service")
    require(provision_handler, "request.TargetAppUserId", "provision-admin-user endpoint uses target app user id")
    require(provision_handler, "null,", "provision-admin-user endpoint does not trust request email/normalized email")
    require(provision_handler, "Results.Ok(response) : Results.Conflict(response)", "provision-admin-user endpoint maps success and safe failure responses")
    require(admin_role_provision_http_request, "public sealed class AdminRoleAssignmentProvisionAdminUserRequest", "provision-admin-user request contract")
    for field in ["Guid TargetAppUserId", "string? Reason", "string? SafeMetadataJson"]:
        require(admin_role_provision_http_request, field, f"provision-admin-user request field {field}")
    for forbidden in ["TargetAdminUserId", "AppUserId", "NormalizedEmail", "Email", "TargetEmail", "ActorAdminUserId", "ActorRoleIds", "RoleId", "Password", "Token", "InviteToken", "CreatedBy", "AssignedByAdminUserId", "DisabledByAdminUserId"]:
        if forbidden in admin_role_provision_http_request.replace("TargetAppUserId", ""):
            raise AssertionError(f"Provision-admin-user request must not accept forbidden field: {forbidden}.")
    for field in ["bool Success", "string? ErrorCode", "string? Message", "Guid? AdminUserId", "Guid? AuditEventId", "DateTimeOffset OccurredAtUtc"]:
        require(admin_role_provision_http_response, field, f"safe provision-admin-user response field {field}")
    for forbidden in ["Email", "Claims", "Token", "Raw", "MetadataJson", "Exception", "ConnectionString", "ProviderPayload"]:
        if forbidden in admin_role_provision_http_response:
            raise AssertionError(f"Provision-admin-user response must not expose unsafe field: {forbidden}")
    for forbidden in ["IAdminRoleAssignmentWriteService", "AssignRoleAsync", "RevokeRoleAsync", "DisableAdminAsync", "_dbContext", "AdminUsers", "AdminUserRoles", "new AdminUser", "new AdminUserRole", ".Add(", ".AddAsync(", ".Attach(", ".Update(", ".Remove(", "SaveChanges", "AppendAuditEventAsync", "AdminRoleAssignmentAuditService", "ActorAdminUserId =", "ActorRoleIds ="]:
        if forbidden in provision_handler:
            raise AssertionError(f"Provision-admin-user endpoint must not assign/revoke/disable, mutate EF directly, audit directly, use write service, or trust actor fields: {forbidden}")

    for forbidden_route in ["CreateAdmin", "InviteRoute"]:
        if forbidden_route in api_constants or forbidden_route in admin_endpoints:
            raise AssertionError(f"No disable/create-admin/invite endpoint may exist: {forbidden_route}")
    for route in BOOTSTRAP_REQUIRED_ROUTES:
        if route_to_policy.get(route) != "BootstrapAdminPolicyName":
            raise AssertionError(f"Dangerous/write/billing/CMS/Premium/free-lesson/user-level endpoint must remain BootstrapAdmin: {route}")

    desktop_text = "\n".join(read(path) for path in list_source_files(DESKTOP_ROOT) if "backend/EnglishVoiceTutor.Api" not in path.as_posix())
    admin_ui_text = "\n".join(read(path) for path in list_source_files(ADMIN_UI_ROOT))
    require(admin_ui_text, "Persistent Admin Roles", "Admin UI role management MVP page")
    role_assignment_routes = set(re.findall(r'"(/api/admin/role-assignments/[^"#?]+)"', admin_ui_text))
    expected_role_assignment_routes = {
        "/api/admin/role-assignments/diagnostics",
        "/api/admin/role-assignments/actor",
        "/api/admin/role-assignments/provision-admin-user",
        "/api/admin/role-assignments/assign",
        "/api/admin/role-assignments/revoke",
        "/api/admin/role-assignments/disable-admin",
        "/api/admin/role-assignments/enable-admin",
    }
    if role_assignment_routes != expected_role_assignment_routes:
        raise AssertionError(f"Admin UI role management must reference only allowed role-assignment routes: {sorted(role_assignment_routes)}")
    for forbidden_admin_ui in ["bootstrap-first-owner", "/api/admin/roles"]:
        if forbidden_admin_ui in admin_ui_text:
            raise AssertionError(f"Admin UI role management must not expose forbidden route: {forbidden_admin_ui}")
    for label, text in [("Desktop", desktop_text), ("Admin UI", admin_ui_text)]:
        for forbidden in ["api.paddle.com", "Paddle.Api", "PADDLE_API_KEY", "PADDLE_WEBHOOK_SECRET"]:
            if forbidden in text:
                raise AssertionError(f"{label} must not reference Paddle directly: {forbidden}")

    print("Admin role assignment persistence foundation static checks passed.")


if __name__ == "__main__":
    main()
