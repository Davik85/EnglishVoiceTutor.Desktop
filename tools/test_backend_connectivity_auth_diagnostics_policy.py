#!/usr/bin/env python3
"""Static policy checks for desktop production backend connectivity/auth diagnostics."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]

FILES = {
    "project": ROOT / "EnglishVoiceTutor.Desktop.csproj",
    "constants": ROOT / "Constants" / "BackendConstants.cs",
    "endpoint_builder": ROOT / "Services" / "BackendEndpointBuilder.cs",
    "settings_vm": ROOT / "ViewModels" / "SettingsViewModel.cs",
    "auth_backend": ROOT / "Services" / "Auth" / "AuthBackendService.cs",
    "diagnostics": ROOT / "Services" / "BackendDiagnosticsService.cs",
    "current_state": ROOT / "docs" / "CURRENT_STATE.md",
    "next_steps": ROOT / "docs" / "NEXT_STEPS.md",
    "tester_release": ROOT / "docs" / "TESTER_RELEASE.md",
    "windows_upload": ROOT / "docs" / "WINDOWS_RELEASE_SERVER_UPLOAD.md",
}

PROD_URL = "https://api.languagevoicetutor.com"


def read(name: str) -> str:
    path = FILES[name]
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, re.S):
        raise AssertionError(f"Forbidden {label}: {pattern}")


def main() -> None:
    project = read("project")
    constants = read("constants")
    endpoint_builder = read("endpoint_builder")
    settings_vm = read("settings_vm")
    auth_backend = read("auth_backend")
    diagnostics = read("diagnostics")
    docs = "\n".join(read(name) for name in ["current_state", "next_steps", "tester_release", "windows_upload"])

    assert_contains(constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend constant")
    assert_contains(project, f"<DesktopBackendBaseUrl Condition=\"'$(DesktopBackendBaseUrl)' == '' and '$(Configuration)' != 'Debug'\">{PROD_URL}</DesktopBackendBaseUrl>", "Release backend default")
    assert_contains(project, "'$(Configuration)' == 'Debug'", "Debug-only localhost default")
    release_default_line = re.search(r"<DesktopBackendBaseUrl[^>]*Configuration\)' != 'Debug'[^>]*>(.*?)</DesktopBackendBaseUrl>", project)
    if not release_default_line or "localhost" in release_default_line.group(1).lower() or "127.0.0.1" in release_default_line.group(1):
        raise AssertionError("Release backend default must not be localhost or 127.0.0.1.")

    assert_contains(endpoint_builder, "ResolveBuildDefaultBaseUrl", "build default resolver")
    assert_contains(endpoint_builder, "ResolveSavedBaseUrlForCurrentBuild", "saved backend resolver")
    assert_contains(endpoint_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release endpoint lock ignores overrides")
    assert_contains(settings_vm, "ResolveSavedBaseUrlForCurrentBuild", "settings uses resolved effective backend URL")

    assert_contains(constants, 'RootHealthEndpoint = "/health"', "root health endpoint")
    assert_contains(diagnostics, "BackendStatusCode", "health diagnostics status code")
    assert_contains(diagnostics, "ErrorCategory", "health diagnostics category")
    assert_contains(diagnostics, "BackendConstants.RootHealthEndpoint", "health diagnostics checks root health")
    assert_contains(diagnostics, "BackendConstants.HealthEndpoint", "health diagnostics falls back to api health")
    assert_contains(settings_vm, "RefreshBackendHealthDiagnosticsAsync", "startup/settings health check")
    assert_contains(settings_vm, "BackendStatus = diagnosticsResult.IsBackendHealthy", "health result updates backend status separately")
    assert_contains(settings_vm, "Backend health check", "diagnostics report health result")
    assert_contains(settings_vm, "Last backend error category", "diagnostics report last category")
    assert_contains(settings_vm, "Last backend HTTP status", "diagnostics report last HTTP status")

    assert_contains(auth_backend, "FromHttpFailure", "auth HTTP failure classification")
    assert_contains(auth_backend, "TryReadAuthErrorMessageAsync", "auth safe error parser")
    assert_contains(auth_backend, "AuthErrorResponse", "auth error DTO")
    assert_contains(auth_backend, "HttpStatusCode.Conflict", "duplicate email conflict classification")
    assert_contains(auth_backend, "HttpStatusCode.BadRequest", "validation failure classification")
    assert_contains(auth_backend, "HttpStatusCode.Unauthorized", "invalid credentials classification")
    assert_contains(settings_vm, "BuildAuthFailureMessage", "auth UI safe failure mapper")
    assert_contains(settings_vm, "result.Message", "auth UI preserves backend safe message")

    assert_contains(settings_vm, "await LoadSettingsForCurrentSessionAsync();", "settings load remains post-auth non-blocking")
    register_login_region = re.search(r"private async Task RegisterAsync\(\).*?private void ShowPasswordResetPanel", settings_vm, re.S)
    if not register_login_region:
        raise AssertionError("Could not locate registration/login region.")
    if "LoadSettingsForCurrentSessionAsync" in register_login_region.group(0):
        raise AssertionError("Registration/login must not require loading settings before auth calls.")

    guarded_files = [settings_vm, auth_backend, diagnostics]
    for text in guarded_files:
        assert_not_regex(text, r"\.GetAwaiter\(\)\.GetResult\(\)|\.Wait\s*\(|\.Result\b", "blocking async pattern in backend connectivity/auth/settings path")
    assert_contains(auth_backend, "return AuthMeResult.BackendUnavailable();", "backend outage does not clear auth session")

    assert_contains(docs, PROD_URL, "docs production tester backend URL")
    assert_contains(docs, "Clean-machine smoke", "docs clean-machine smoke checklist")
    assert_contains(docs, "second Windows device", "docs second-device verification block")

    print("Backend connectivity/auth diagnostics policy checks passed.")


if __name__ == "__main__":
    main()
