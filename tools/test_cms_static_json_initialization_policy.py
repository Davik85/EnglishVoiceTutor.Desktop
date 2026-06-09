#!/usr/bin/env python3
"""Static policy checks for safe CMS static-json-v1 initialization."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]

FILES = {
    "api_constants": ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs",
    "admin_endpoints": ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs",
    "import_interface": ROOT / "backend/EnglishVoiceTutor.Api/Services/Cms/ICmsContentImportService.cs",
    "import_service": ROOT / "backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentImportService.cs",
    "import_models": ROOT / "backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentImportModels.cs",
    "admin_html": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html",
    "admin_js": ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js",
    "runbook": ROOT / "docs/CMS_ADMIN_SERVER_VERIFICATION.md",
    "current_state": ROOT / "docs/CURRENT_STATE.md",
    "next_steps": ROOT / "docs/NEXT_STEPS.md",
    "tester_release": ROOT / "docs/TESTER_RELEASE.md",
    "plan": ROOT / "docs/cms-content-mvp-plan.md",
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
    api_constants = read("api_constants")
    admin_endpoints = read("admin_endpoints")
    import_interface = read("import_interface")
    import_service = read("import_service")
    import_models = read("import_models")
    admin_html = read("admin_html")
    admin_js = read("admin_js")

    assert_contains(api_constants, "AdminDevCmsStaticJsonV1InitializeRoute", "static-json-v1 initialize route constant")
    assert_contains(api_constants, "/api/admin/dev/cms/content-packs/static-json-v1/initialize-from-static-json", "initialize endpoint path")
    assert_contains(admin_endpoints, "InitializeStaticJsonV1CmsContentPackAsync", "initialize endpoint handler")
    assert_contains(admin_endpoints, "app.MapPost(ApiConstants.AdminDevCmsStaticJsonV1InitializeRoute", "POST initialize endpoint mapping")
    initialize_mapping = admin_endpoints.split("app.MapPost(ApiConstants.AdminDevCmsStaticJsonV1InitializeRoute", 1)[1].split(";", 1)[0]
    assert_contains(initialize_mapping, "RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)", "BootstrapAdmin authorization on initialize endpoint")

    assert_contains(import_interface, "InitializeStaticJsonV1DraftAsync", "import service initialize contract")
    assert_contains(import_service, "InitializeStaticJsonV1DraftAsync", "import service initialize implementation")
    assert_contains(import_service, "CmsContentConstants.ContentPackStatuses.Draft", "initialize creates draft content pack status")
    assert_contains(import_service, "HasAnyDraftContentAsync", "draft preservation guard")
    assert_contains(import_service, "Draft preserved.", "draft preserved message")
    assert_contains(import_service, "Learner runtime was not changed", "runtime unchanged message")
    initialize_method = import_service.split("InitializeStaticJsonV1DraftAsync", 1)[1].split("private static async Task<CmsStaticContentImportDraft>", 1)[0]
    assert_not_contains(initialize_method, "PublishSnapshotIfChangedAsync", "initialize endpoint must not publish")
    assert_not_contains(initialize_method, "ContentVersions.Add", "initialize endpoint must not create content versions")
    assert_contains(import_models, "RuntimeUnchanged", "result reports runtime unchanged")
    assert_contains(import_models, "DraftPreserved", "result reports draft preserved")

    assert_contains(admin_html, "Initialize from static JSON", "UI initialize button")
    assert_contains(admin_html, "Content pack static-json-v1 has not been initialized in CMS yet.", "specific missing-pack UI explanation")
    assert_contains(admin_html, "does not publish automatically and does not change learner runtime", "UI runtime warning")
    assert_contains(admin_js, "cmsStaticJsonV1Initialize", "UI initialize API path")
    assert_contains(admin_js, "initializeStaticJsonContentPack", "UI initialize handler")
    assert_contains(admin_js, "Content pack static-json-v1 has not been initialized in CMS yet", "UI missing selected pack message")
    assert_contains(admin_js, "Learner runtime remains static JSON; no publish was performed", "UI success runtime/no-publish message")
    assert_not_contains(admin_js, "setCmsError(\"CMS item was not found.\")", "vague missing pack top-level UI error")

    for name in ["runbook", "current_state", "next_steps", "tester_release", "plan"]:
        text = read(name)
        assert_contains(text, "Initialize from static JSON", f"{name} initialization action docs")
        assert_contains(text, "does not switch runtime", f"{name} runtime safety docs")
        assert_contains(text, "CmsContent__UsePublishedSnapshotForRuntime=true", f"{name} explicit runtime switch docs")

    for path in FILES.values():
        assert_no_real_secrets(path)

    print("CMS static-json-v1 initialization policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
