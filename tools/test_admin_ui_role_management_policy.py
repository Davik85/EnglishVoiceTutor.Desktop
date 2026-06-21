#!/usr/bin/env python3
"""Static checks for the Admin UI role-management MVP."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
ADMIN_INDEX = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
RELEASE_GATE = ROOT / "tools/run_desktop_release_gate.ps1"
ROLE_MANAGEMENT_SMOKE = ROOT / "tools/smoke_admin_role_management_flow.ps1"
ROLE_MANAGEMENT_RUNBOOK = ROOT / "docs/ADMIN_ROLE_MANAGEMENT_UI_RUNBOOK.md"
HANDLER = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminPermissionAuthorizationHandler.cs"
ENDPOINTS = ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs"

ALLOWED_ENDPOINTS = {
    "/api/admin/role-assignments/actor",
    "/api/admin/role-assignments/diagnostics",
    "/api/admin/role-assignments/provision-admin-user",
    "/api/admin/role-assignments/assign",
    "/api/admin/role-assignments/revoke",
    "/api/admin/role-assignments/disable-admin",
    "/api/admin/role-assignments/enable-admin",
}
FORBIDDEN_TERMS = [
    "bootstrap-first-owner",
    "paddle",
    "/billing/",
    "premium-grants",
    "free-lesson-allowance/reset",
    "/publish",
    "/restore",
    "initialize-from-static-json",
]
PROVISION_FORBIDDEN_NAMES = ["email", "normalizedEmail", "password", "token", "inviteToken", "actorAdminUserId", "actorRoleIds", "roleId"]
ACTOR_FORBIDDEN_NAMES = ["actorAdminUserId", "actorRoleIds"]

SMOKE_FORBIDDEN_TERMS = [
    "bootstrap-first-owner",
    "paddle",
    "/billing/",
    "premium-grants",
    "free-lesson-allowance/reset",
    "/publish",
    "/restore",
    "initialize-from-static-json",
]
SECRET_PATTERNS = [
    r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
    r"Bearer\s+[A-Za-z0-9._~+/=-]+",
    r"(Password|Cookie|ConnectionString)\s*=\s*['\"][^'\"]+['\"]",
]


def read(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def form_block(index: str, form_id: str) -> str:
    match = re.search(rf'<form id="{re.escape(form_id)}".*?</form>', index, flags=re.DOTALL)
    if not match:
        raise AssertionError(f"Missing form {form_id}")
    return match.group(0)


def main() -> None:
    admin_js = read(ADMIN_JS)
    index = read(ADMIN_INDEX)
    release_gate = read(RELEASE_GATE)
    role_management_smoke = read(ROLE_MANAGEMENT_SMOKE)
    role_management_runbook = read(ROLE_MANAGEMENT_RUNBOOK)
    handler = read(HANDLER)
    endpoints = read(ENDPOINTS)

    if "Persistent Admin Roles" not in index or "tab-button-role-management" not in index:
        raise AssertionError("Admin UI role management page/navigation is missing")

    role_endpoint_refs = set(re.findall(r'"(/api/admin/role-assignments/[^"]+)"', admin_js))
    if role_endpoint_refs != ALLOWED_ENDPOINTS:
        raise AssertionError(f"Unexpected role-assignment endpoint refs in Admin UI: {sorted(role_endpoint_refs)}")

    panel = re.search(r'<div id="tab-panel-role-management".*?<div id="tab-panel-system"', index, flags=re.DOTALL)
    if not panel:
        raise AssertionError("Missing role management panel block")
    role_ui = panel.group(0) + "\n" + "\n".join(line for line in admin_js.splitlines() if "roleAssignment" in line or "RoleManagement" in line or "roleManagement" in line)
    for term in FORBIDDEN_TERMS:
        if term.lower() in role_ui.lower():
            raise AssertionError(f"Forbidden term referenced by Admin UI role management surface: {term}")

    provision = form_block(index, "role-provision-form")
    for name in PROVISION_FORBIDDEN_NAMES:
        if re.search(rf'name="{re.escape(name)}"', provision):
            raise AssertionError(f"Provision form must not accept {name}")
    for form_id in ["role-assign-form", "role-revoke-form", "role-disable-form", "role-enable-form"]:
        block = form_block(index, form_id)
        for name in ACTOR_FORBIDDEN_NAMES:
            if re.search(rf'name="{re.escape(name)}"', block):
                raise AssertionError(f"{form_id} must not accept {name}")
        if 'name="reason"' not in block or 'required' not in block or 'confirmChange' not in block:
            raise AssertionError(f"{form_id} must require reason and explicit confirmation")

    if "roleManagementForms.forEach" not in admin_js or "addEventListener(\"submit\"" not in admin_js:
        raise AssertionError("Mutating role-management actions must be submit-driven")
    if "loadRoleManagementData" not in admin_js or "Promise.all" not in admin_js:
        raise AssertionError("Role management page must load actor mapping and diagnostics")
    load_body = re.search(r"async function loadRoleManagementData\(\).*?\n    }", admin_js, flags=re.DOTALL)
    if not load_body:
        raise AssertionError("Missing loadRoleManagementData function")
    for forbidden_path in ["roleAssignmentProvisionAdminUser", "roleAssignmentAssign", "roleAssignmentRevoke", "roleAssignmentDisableAdmin", "roleAssignmentEnableAdmin"]:
        if forbidden_path in load_body.group(0):
            raise AssertionError("Mutating role-management endpoints must not be called on page load")
    if "test_admin_ui_role_management_policy.py" not in release_gate:
        raise AssertionError("Release gate must run the Admin UI role-management policy test")
    if "smoke_admin_role_management_flow" in release_gate:
        raise AssertionError("Role-management smoke script must remain opt-in and outside release gate")
    if "smoke_admin_role_assignment_bootstrap_first_owner" in release_gate:
        raise AssertionError("Bootstrap smoke script must remain opt-in and outside release gate")

    for required in [
        'BaseUrl = "http://localhost:5000"',
        "AllowProductionUrl",
        "ConfirmRoleManagementMutations",
        "Refusing to run against production-looking URL without -AllowProductionUrl",
        "Refusing to run against non-local URL without -AllowProductionUrl",
        "Assert-MutationReason",
        "Read-only role-management smoke completed. No mutating role-management calls were made.",
        "Write-SafeResult",
        '$ActorPath = "/api/admin/role-assignments/actor"',
        '$DiagnosticsPath = "/api/admin/role-assignments/diagnostics"',
        '$ProvisionAdminUserPath = "/api/admin/role-assignments/provision-admin-user"',
        '$AssignRolePath = "/api/admin/role-assignments/assign"',
        '$RevokeRolePath = "/api/admin/role-assignments/revoke"',
        '$DisableAdminPath = "/api/admin/role-assignments/disable-admin"',
        '$EnableAdminPath = "/api/admin/role-assignments/enable-admin"',
    ]:
        if required not in role_management_smoke:
            raise AssertionError(f"Role-management smoke script missing required safety/flow marker: {required}")
    mutation_marker = "if (-not $ConfirmRoleManagementMutations)"
    if mutation_marker not in role_management_smoke:
        raise AssertionError("Role-management smoke script must branch on explicit mutation confirmation")
    for term in SMOKE_FORBIDDEN_TERMS:
        if term.lower() in role_management_smoke.lower():
            raise AssertionError(f"Forbidden endpoint/domain referenced by role-management smoke script: {term}")
    for pattern in SECRET_PATTERNS:
        if re.search(pattern, role_management_smoke):
            raise AssertionError(f"Role-management smoke script appears to contain an embedded secret-like value: {pattern}")
    for unsafe_output in ["accessToken | Write-Host", "refreshToken", "AdminPassword | Write-Host", "SafeMetadataJson | Write-Host", "Cookie", "Headers | Write-Host", "Exception | Write-Host"]:
        if unsafe_output in role_management_smoke:
            raise AssertionError(f"Role-management smoke script must not print unsafe output marker: {unsafe_output}")
    if "accessToken" not in role_management_smoke or 'Authorization = "Bearer $($login.accessToken)"' not in role_management_smoke:
        raise AssertionError("Role-management smoke script must use but not print the login access token")

    for required in [
        "controlled tester/admin operational validation only",
        "not a broad public production cutover",
        "bootstrap-first-owner` remains manual/runbook-controlled and is not exposed in the Admin UI",
        "<ADMIN_EMAIL>",
        "<ADMIN_PASSWORD>",
        "<TARGET_APP_USER_ID>",
        "<TARGET_ADMIN_USER_ID>",
        "<ROLE_ID>",
        "smoke_admin_role_management_flow.ps1",
    ]:
        if required not in role_management_runbook:
            raise AssertionError(f"Admin role-management UI runbook missing required statement/placeholder: {required}")
    for pattern in SECRET_PATTERNS:
        matches = [m.group(0) for m in re.finditer(pattern, role_management_runbook)]
        allowed = {"<ADMIN_EMAIL>"}
        if any(match not in allowed for match in matches):
            raise AssertionError(f"Runbook appears to contain a real secret-like value: {matches}")

    if "roleAssignment" in index.lower() and "bootstrap-first-owner" in index:
        raise AssertionError("bootstrap-first-owner must not be exposed in Admin UI")
    for required in ["AdminRoleAssignmentReadService", "BootstrapAdmin"]:
        if required not in handler:
            raise AssertionError(f"AdminPermissionAuthorizationHandler must preserve persistent-role evaluation/fallback marker: {required}")
    for forbidden in ["IAdminRoleAssignmentWriteService", "IAdminRoleAssignmentSafetyService", "IAdminRoleAssignmentAuditService", "IAdminRoleAssignmentActorResolver", "IAdminRoleAssignmentBootstrapService", "IAdminRoleAssignmentAdminUserProvisioningService"]:
        if forbidden in handler:
            raise AssertionError(f"AdminPermissionAuthorizationHandler must not use write/safety/audit/actor/bootstrap/provisioning services: {forbidden}")
    if endpoints.count("AdminRoleManagementPermissionPolicyName") < 8:
        raise AssertionError("Role-assignment management endpoints must remain protected by AdminRoleManagementPermissionPolicyName")


if __name__ == "__main__":
    main()
