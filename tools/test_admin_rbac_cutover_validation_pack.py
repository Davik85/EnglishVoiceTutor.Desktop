#!/usr/bin/env python3
"""Static checks for the manual Admin RBAC cutover validation pack."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tools/smoke_admin_rbac_cutover_validation.ps1"
RUNBOOK = ROOT / "docs/ADMIN_RBAC_CUTOVER_RUNBOOK.md"
RELEASE_GATE = ROOT / "tools/run_desktop_release_gate.ps1"
ADMIN_ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"
ADMIN_UI_ROOT = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin"

SAFE_ENDPOINTS = {
    "/health",
    "/api/auth/login",
    "/api/admin/me",
    "/api/admin/capabilities",
    "/api/admin/statistics/overview",
    "/api/admin/dev/cms/runtime-status",
    "/api/admin/role-assignments/actor",
    "/api/admin/role-assignments/diagnostics",
    "/api/admin/rbac/cutover-status",
}

FORBIDDEN_SCRIPT_ENDPOINT_PARTS = [
    "premium", "free-lesson", "cancel-renewal", "draft", "save", "import", "initialize",
    "init", "validate", "preview", "publish", "restore", "assign", "revoke", "disable-admin",
    "enable-admin", "provision-admin-user", "bootstrap-first-owner", "paddle", "billing/provider",
]

FORBIDDEN_SECRET_PATTERNS = [
    r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
    r"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
    r"(Server|Host|Data Source)\s*=.+;(User Id|Username|Password)\s*=",
    r"BEGIN (RSA |EC |OPENSSH |)PRIVATE KEY",
]


def read(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle.lower() in text.lower():
        raise AssertionError(f"Forbidden {label}: {needle}")


def list_source_files(root: Path) -> list[Path]:
    return [path for path in root.rglob("*") if path.is_file() and path.suffix.lower() in {".cs", ".xaml", ".js", ".html", ".ts", ".tsx", ".jsx", ".json", ".xml", ".config"}]


def main() -> None:
    script = read(SCRIPT)
    runbook = read(RUNBOOK)
    release_gate = read(RELEASE_GATE)
    admin_endpoints = read(ADMIN_ENDPOINTS)

    require(release_gate, "Admin RBAC permission policy foundation", "Admin RBAC permission policy foundation release-gate section")
    require(release_gate, "tools/test_admin_rbac_permission_policy_foundation.py", "Admin RBAC permission policy foundation release-gate test")
    require(release_gate, "Admin role assignment persistence foundation", "Admin role assignment persistence foundation release-gate section")
    require(release_gate, "tools/test_admin_role_assignment_persistence_foundation.py", "Admin role assignment persistence foundation release-gate test")
    require(release_gate, "Admin UI role management policy", "Admin UI role-management release-gate section")
    require(release_gate, "tools/test_admin_ui_role_management_policy.py", "Admin UI role-management release-gate test")
    require(release_gate, "Admin RBAC cutover validation pack", "Admin RBAC cutover validation pack release-gate section")
    require(release_gate, "tools/test_admin_rbac_cutover_validation_pack.py", "Admin RBAC cutover validation pack release-gate test")
    require(release_gate, "Admin roles permissions policy", "Admin roles permissions policy release-gate section")
    require(release_gate, "tools/test_admin_roles_permissions_policy.py", "Admin roles permissions policy release-gate test")

    for smoke_script in [
        "tools/smoke_admin_rbac_cutover_validation.ps1",
        "tools/smoke_admin_role_management_flow.ps1",
        "tools/smoke_admin_role_assignment_bootstrap_first_owner.ps1",
    ]:
        if smoke_script in release_gate or Path(smoke_script).name in release_gate:
            raise AssertionError(f"Manual opt-in smoke script must not be executed by the desktop release gate: {smoke_script}")

    require(script, '[string]$BaseUrl = "http://localhost:5000"', "localhost default BaseUrl")
    require(script, "AllowProductionUrl", "explicit production/non-local override switch")
    require(script, "ConfirmRbacCutoverValidation", "explicit cutover confirmation switch")
    require(script, "The script stopped before making Admin requests", "safe failure before Admin requests without confirmation")
    require(script, "AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies", "fallback setting name")
    require(script, "Refusing to run against a non-local URL without -AllowProductionUrl", "non-local URL refusal")
    require(script, "Refusing to run against a production-looking URL without -AllowProductionUrl", "production-looking URL refusal")
    require(script, "$HealthPath = \"/health\"", "health reachability check")
    require(script, "$AuthLoginPath = \"/api/auth/login\"", "existing auth login endpoint")
    require(script, "$RbacCutoverStatusPath = \"/api/admin/rbac/cutover-status\"", "RBAC cutover status endpoint")
    require(script, "bootstrapAdminFallbackForAdminPermissionPoliciesEnabled", "fallback status comparison field")
    if ".HasValue" in script:
        raise AssertionError("Cutover smoke script must not use Nullable.HasValue; use explicit PSBoundParameters presence checks.")
    expected_value_member_pattern = re.compile(r"\$Expected[A-Za-z0-9_]+\.Value")
    if expected_value_member_pattern.search(script):
        raise AssertionError("Expected* parameters must not use nullable .Value; normalize explicit parameter values instead.")

    expected_parameters = set(re.findall(r"\[object\]\$(Expected[A-Za-z0-9_]+)", script))
    required_expected_parameters = {
        "ExpectedFallbackEnabled",
        "ExpectedActorMappingFound",
        "ExpectedAdminPermissionEndpointStatus",
        "ExpectedRoleManagementEndpointStatus",
    }
    if expected_parameters != required_expected_parameters:
        raise AssertionError(f"Unexpected Expected* parameter set: {sorted(expected_parameters)}")

    for parameter_name in sorted(required_expected_parameters):
        require(script, f"$PSBoundParameters.ContainsKey('{parameter_name}')", f"robust {parameter_name} provided-value check")

    require(script, "ConvertTo-OptionalBooleanParameter", "boolean Expected* normalization helper")
    for boolean_parameter in ["ExpectedFallbackEnabled", "ExpectedActorMappingFound"]:
        require(script, f'ConvertTo-OptionalBooleanParameter -ParameterName "{boolean_parameter}"', f"{boolean_parameter} boolean normalization")
    for supported_value in ['"true"', '"false"', '"1"', '"0"']:
        require(script, supported_value, f"boolean Expected* supported value {supported_value}")
    require(script, "ConvertTo-ExpectedHttpStatusParameter", "HTTP status Expected* normalization helper")
    for status_parameter in ["ExpectedAdminPermissionEndpointStatus", "ExpectedRoleManagementEndpointStatus"]:
        require(script, f'ConvertTo-ExpectedHttpStatusParameter -ParameterName "{status_parameter}"', f"{status_parameter} HTTP status normalization")
    require(script, "expectedFallbackEnabledValue", "ExpectedFallbackEnabled normalized value is compared with backend status")
    require(script, "expectedActorMappingFoundValue", "ExpectedActorMappingFound normalized value is compared with backend status")
    require(script, "isActorMappingFound", "backend-reported actor mapping state is read safely")

    endpoints = set(re.findall(r'"(/[A-Za-z0-9_./{}?-]+)"', script))
    unexpected = sorted(endpoint for endpoint in endpoints if endpoint.startswith("/") and endpoint not in SAFE_ENDPOINTS)
    if unexpected:
        raise AssertionError(f"Cutover smoke script calls unexpected endpoints: {unexpected}")
    missing = sorted(SAFE_ENDPOINTS - endpoints)
    if missing:
        raise AssertionError(f"Cutover smoke script is missing expected safe endpoints: {missing}")

    forbid(script, "api.paddle.com", "Paddle endpoint reference in cutover smoke script")

    for pattern in FORBIDDEN_SECRET_PATTERNS:
        if re.search(pattern, script):
            raise AssertionError(f"Cutover smoke script appears to contain a secret, real email, token, certificate, or connection string pattern: {pattern}")

    for unsafe_print in [
        "Write-Host $AdminEmail", "Write-Host $AdminPassword", "Write-Host $headers", "Write-Host $login",
        "ConvertTo-Json -Depth 5 | Write-Host", "response.Content", "RawContent", "Set-Cookie", "Cookie",
    ]:
        if unsafe_print in script:
            raise AssertionError(f"Cutover smoke script must not print unsafe data: {unsafe_print}")

    require(runbook, "owner-approved controlled validation only", "owner-approved controlled validation warning")
    require(runbook, "fallback remains enabled by default", "fallback enabled by default documentation")
    require(runbook, "AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies", "fallback setting documentation")
    require(runbook, "back to `true`, or remove the setting", "rollback by true or removing setting")
    require(runbook, "<admin-email>", "placeholder admin email")
    require(runbook, "<admin-password>", "placeholder admin password")
    for pattern in FORBIDDEN_SECRET_PATTERNS:
        if re.search(pattern, runbook.replace("<admin-email>", "")):
            raise AssertionError(f"Runbook appears to contain a non-placeholder secret/email/token pattern: {pattern}")

    permission_authorizations = re.findall(
        r"RequireAuthorization\(AdminAuthorizationConstants\.([A-Za-z0-9_]+PermissionPolicyName)\)",
        admin_endpoints,
    )
    admin_permission_count = len([
        policy for policy in permission_authorizations
        if policy != "AdminRoleManagementPermissionPolicyName"
    ])
    if admin_permission_count != 37:
        raise AssertionError(f"AdminPermission endpoint count must remain exactly 37, including the POST-only Admin-created account-deletion request endpoint; got {admin_permission_count}.")

    admin_activity_pattern = r"MapGet\(ApiConstants\.AdminActivityRoute,.*?RequireAuthorization\(AdminAuthorizationConstants\.AuditLogViewPermissionPolicyName\)"
    if not re.search(admin_activity_pattern, admin_endpoints, re.DOTALL):
        raise AssertionError("Admin Activity must be present as GET AdminActivityRoute protected by AuditLogViewPermissionPolicyName.")
    if re.search(r"Map(Post|Put|Patch|Delete)\(ApiConstants\.AdminActivityRoute", admin_endpoints):
        raise AssertionError("Admin Activity must remain read-only and must not expose POST, PUT, PATCH, or DELETE mappings.")
    target_audit_pattern = r"MapGet\(ApiConstants\.AdminUserAuditActionsRoute,.*?RequireAuthorization\(AdminAuthorizationConstants\.AuditLogViewPermissionPolicyName\)"
    if not re.search(target_audit_pattern, admin_endpoints, re.DOTALL):
        raise AssertionError("Existing target-user Audit Log endpoint must remain GET-only with AuditLogViewPermissionPolicyName; Admin Activity must not replace or weaken it.")
    account_deletion_request_pattern = r"MapPost\(ApiConstants\.AdminUserAccountDeletionRequestsRoute,.*?RequireAuthorization\(AdminAuthorizationConstants\.AccountAnonymizationExecutePermissionPolicyName\)"
    if not re.search(account_deletion_request_pattern, admin_endpoints, re.DOTALL):
        raise AssertionError("Admin-created account-deletion requests must remain POST-only with AccountAnonymizationExecutePermissionPolicyName.")
    if re.search(r"Map(Get|Put|Patch|Delete)\(ApiConstants\.AdminUserAccountDeletionRequestsRoute", admin_endpoints):
        raise AssertionError("Admin-created account-deletion requests must not expose GET, PUT, PATCH, or DELETE mappings.")
    account_deletion_request_start = admin_endpoints.index("app.MapPost(ApiConstants.AdminUserAccountDeletionRequestsRoute")
    account_deletion_request_end = admin_endpoints.find("app.Map", account_deletion_request_start + 1)
    account_deletion_request_registration = admin_endpoints[account_deletion_request_start:account_deletion_request_end]
    if "BootstrapAdminPolicyName" in account_deletion_request_registration:
        raise AssertionError("Admin-created account-deletion requests must not use BootstrapAdminPolicyName directly.")

    for route in [
        "AdminRoleAssignmentDiagnosticsRoute", "AdminRoleAssignmentActorRoute", "AdminRoleAssignmentRevokeRoute",
        "AdminRoleAssignmentAssignRoute", "AdminRoleAssignmentDisableAdminRoute", "AdminRoleAssignmentEnableAdminRoute",
        "AdminRoleAssignmentProvisionAdminUserRoute", "AdminRoleAssignmentBootstrapFirstOwnerRoute",
    ]:
        pattern = rf"ApiConstants\.{route}.*?RequireAuthorization\(AdminAuthorizationConstants\.AdminRoleManagementPermissionPolicyName\)"
        if not re.search(pattern, admin_endpoints, re.DOTALL):
            raise AssertionError(f"Role-assignment route must remain protected by AdminRoleManagementPermissionPolicyName: {route}")

    for route in ["AdminDevCmsStaticContentImportRoute", "AdminDevCmsStaticJsonV1InitializeRoute"]:
        pattern = rf"ApiConstants\.{route}.*?RequireAuthorization\(AdminAuthorizationConstants\.BootstrapAdminPolicyName\)"
        if not re.search(pattern, admin_endpoints, re.DOTALL):
            raise AssertionError(f"CMS import/init route must remain BootstrapAdmin-protected: {route}")

    admin_ui_text = "\n".join(read(path) for path in list_source_files(ADMIN_UI_ROOT))
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
        raise AssertionError(f"Admin UI role management scope changed: {sorted(role_assignment_routes)}")

    desktop_files = [
        path for path in list_source_files(ROOT)
        if "backend" not in path.parts and "docs" not in path.parts and "tools" not in path.parts and ".git" not in path.parts
    ]
    desktop_text = "\n".join(read(path) for path in desktop_files)
    for label, text in [("Desktop", desktop_text), ("Admin UI", admin_ui_text)]:
        for forbidden in ["api.paddle.com", "Paddle.Api", "PADDLE_API_KEY", "PADDLE_WEBHOOK_SECRET"]:
            forbid(text, forbidden, f"direct Paddle reference in {label}")

    print("Admin RBAC cutover validation pack static checks passed.")


if __name__ == "__main__":
    main()
