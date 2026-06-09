#!/usr/bin/env python3
"""Static policy checks for CMS/Admin production server verification readiness."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]

FILES = {
    "bootstrap_service": ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/BootstrapAdminAccessService.cs",
    "admin_endpoints": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs",
    "api_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs",
    "program": ROOT / "backend/EnglishVoiceTutor.Api/Program.cs",
    "diagnostics_endpoint": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/CmsDiagnosticsEndpoints.cs",
    "diagnostics_contract": ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Cms/CmsRuntimeContentSourceStatusResponse.cs",
    "runbook": ROOT / "docs/CMS_ADMIN_SERVER_VERIFICATION.md",
    "current_state": ROOT / "docs/CURRENT_STATE.md",
    "next_steps": ROOT / "docs/NEXT_STEPS.md",
    "tester_release": ROOT / "docs/TESTER_RELEASE.md",
    "helper": ROOT / "tools/verify_cms_admin_server_readiness.ps1",
}

SECRET_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|secret|password|passwd|pwd|token)\s*[:=]\s*['\"][^'\"]+['\"]"),
    re.compile(r"(?i)(bearer|basic)\s+[a-z0-9._~+/=-]{16,}"),
    re.compile(r"(?i)-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----"),
    re.compile(r"sk-[A-Za-z0-9_-]{16,}"),
]

ALLOWLIST_SNIPPETS = [
    "EVT_ADMIN_BEARER_TOKEN",
    "EVT_NON_ADMIN_BEARER_TOKEN",
    "<paste short-lived admin JWT only in your shell history-safe workflow>",
    "AdminBearerToken = $env:EVT_ADMIN_BEARER_TOKEN",
    "NonAdminBearerToken = $env:EVT_NON_ADMIN_BEARER_TOKEN",
    "password reset/change flows are working",
]


def read(name: str) -> str:
    path = FILES[name]
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def assert_no_real_secrets(path: pathlib.Path) -> None:
    text = path.read_text(encoding="utf-8")
    scrubbed = text
    for snippet in ALLOWLIST_SNIPPETS:
        scrubbed = scrubbed.replace(snippet, "")
    for pattern in SECRET_PATTERNS:
        match = pattern.search(scrubbed)
        if match:
            raise AssertionError(f"Potential secret in {path.relative_to(ROOT)}: {match.group(0)}")


def main() -> int:
    bootstrap = read("bootstrap_service")
    admin_endpoints = read("admin_endpoints")
    api_constants = read("api_constants")
    program = read("program")
    diagnostics_endpoint = read("diagnostics_endpoint")
    diagnostics_contract = read("diagnostics_contract")
    runbook = read("runbook")
    current_state = read("current_state")
    next_steps = read("next_steps")
    tester_release = read("tester_release")
    helper = read("helper")

    assert_not_contains(bootstrap, "IsDevelopment()", "development-only bootstrap admin gate")
    assert_contains(bootstrap, "if (!_options.Enabled)", "explicit AdminBootstrap enabled gate")
    assert_contains(bootstrap, "AdminEmails", "bootstrap admin email allow-list")

    cms_mapping_prefix = admin_endpoints.split("AdminDevCmsStaticContentImportRoute", 1)[0]
    assert_not_contains(cms_mapping_prefix, "IsDevelopment()", "development-only CMS endpoint map gate")
    assert_contains(admin_endpoints, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "CMS admin authorization policy")
    assert_contains(admin_endpoints, "AdminDevCmsRuntimeContentStatusRoute", "admin runtime status endpoint")
    assert_contains(admin_endpoints, "AdminDevCmsContentPackPublishRoute", "publish endpoint")
    assert_contains(admin_endpoints, "AdminDevCmsContentPackVersionRestoreRoute", "restore endpoint")

    assert_contains(api_constants, "CmsRuntimeContentSourceStatusRoute", "public CMS source status route")
    assert_contains(program, "app.MapCmsDiagnosticsEndpoints();", "CMS diagnostics endpoint mapping")
    assert_contains(diagnostics_endpoint, "CmsRuntimeContentSourceStatusResponse", "diagnostics response")
    assert_contains(diagnostics_endpoint, "CmsContentConstants.Sources.StaticJson", "static JSON source reporting")
    assert_contains(diagnostics_endpoint, "CmsContentConstants.Sources.CmsPublishedSnapshot", "CMS snapshot source reporting")
    for forbidden in ["Prompt", "Email", "Token", "ConnectionString", "Password", "ApiKey"]:
        assert_not_contains(diagnostics_contract, forbidden, "secret/content field in public diagnostic contract")

    for text, label in [
        (runbook, "runbook"),
        (current_state, "current state"),
        (next_steps, "next steps"),
        (tester_release, "tester release"),
    ]:
        assert_contains(text, "CMS/Admin", f"{label} CMS/Admin status")
        assert_contains(text, "static JSON", f"{label} static JSON default")
        assert_contains(text, "Public release", f"{label} public release blocker")

    for needle in [
        "AdminBootstrap__Enabled=true",
        "AdminBootstrap__AdminEmails__0=admin@example.com",
        "CmsContent__ReadPublishedSnapshotEnabled=true",
        "CmsContent__UsePublishedSnapshotForRuntime=false",
        "CmsContent__ContentPackSlug=static-json-v1",
        "CmsContent__FallbackToStaticJson=true",
        "sudo systemctl restart languagevoicetutor-backend",
        "/api/admin/dev/cms/runtime-content/status",
        "/api/cms/runtime-content/source-status",
        "No EF schema change is required",
    ]:
        assert_contains(runbook, needle, f"runbook required detail {needle}")

    for needle in [
        "same installed version: ask the user to confirm reinstall",
        "older installed version: allow the guided update flow",
        "newer installed version: warn and block",
        "never auto-update during an active lesson",
    ]:
        assert_contains(next_steps, needle, f"update version rule {needle}")

    assert_contains(helper, "[switch]$MutatingChecks", "helper safe explicit mutating switch")
    assert_contains(helper, "Skipping authenticated admin API checks", "helper token-optional behavior")
    assert_not_contains(helper, "AdminPassword", "helper must not accept or store admin password")

    for path in FILES.values():
        assert_no_real_secrets(path)

    print("CMS/Admin server readiness policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
