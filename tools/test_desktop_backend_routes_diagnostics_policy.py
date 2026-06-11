#!/usr/bin/env python3
"""Policy checks for desktop backend route composition and installed-build request diagnostics."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.exists():
        raise AssertionError(f"Missing required file: {relative}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    constants = read("Constants/BackendConstants.cs")
    storage = read("Constants/StorageConstants.cs")
    builder = read("Services/BackendEndpointBuilder.cs")
    diagnostics = read("Services/BackendDiagnosticsService.cs")
    request_diag = read("Services/BackendRequestDiagnosticsService.cs")
    lesson_backend = read("Services/LessonChatBackendService.cs")
    auth = read("Services/Auth/AuthBackendService.cs")
    settings_client = read("Services/BackendUserSettingsClient.cs")
    subscription = read("Services/BackendSubscriptionStatusClient.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    smoke = read("tools/smoke_desktop_backend_routes.ps1")

    assert_contains(constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend default")
    assert_contains(constants, 'RootHealthEndpoint = "/health"', "root health route")
    assert_contains(constants, 'DatabaseHealthEndpoint = "/api/health/database"', "database health route")
    assert_contains(constants, 'AuthRegisterEndpoint = "/api/auth/register"', "auth register route")
    assert_contains(constants, 'AuthLoginEndpoint = "/api/auth/login"', "auth login route")
    assert_contains(constants, 'AuthMeEndpoint = "/api/auth/me"', "auth me route")

    expected_absolute_urls = [
        "https://api.languagevoicetutor.com/health",
        "https://api.languagevoicetutor.com/api/health/database",
        "https://api.languagevoicetutor.com/api/auth/register",
        "https://api.languagevoicetutor.com/api/auth/login",
        "https://api.languagevoicetutor.com/api/auth/me",
        "https://api.languagevoicetutor.com/api/me/settings",
        "https://api.languagevoicetutor.com/api/me/subscription-status",
    ]
    route_doc = read("docs/DESKTOP_BACKEND_ROUTE_SMOKE.md")
    for expected_url in expected_absolute_urls:
        assert_contains(route_doc, expected_url, f"documented generated absolute URL {expected_url}")

    route_builder_cases = [
        ("https://api.languagevoicetutor.com", "/health", "https://api.languagevoicetutor.com/health"),
        ("https://api.languagevoicetutor.com/", "health", "https://api.languagevoicetutor.com/health"),
        ("https://api.languagevoicetutor.com/api", "/health", "https://api.languagevoicetutor.com/health"),
        ("https://api.languagevoicetutor.com/api/", "api/auth/register", "https://api.languagevoicetutor.com/api/auth/register"),
    ]
    for base_url, endpoint_path, expected_url in route_builder_cases:
        normalized_base = base_url.split("?", 1)[0].rstrip("/")
        scheme, rest = normalized_base.split("://", 1)
        host = rest.split("/", 1)[0]
        actual_url = f"{scheme}://{host}/{endpoint_path.lstrip('/')}"
        if actual_url != expected_url:
            raise AssertionError(f"Route builder case failed: {base_url!r} + {endpoint_path!r} produced {actual_url!r}, expected {expected_url!r}")

    assert_contains(builder, "Path = string.Empty", "base URL path stripping")
    assert_contains(builder, "Query = string.Empty", "base URL query stripping")
    assert_contains(builder, "Fragment = string.Empty", "base URL fragment stripping")
    assert_contains(builder, "endpointPath.TrimStart('/')", "endpoint leading slash normalization")
    assert_contains(builder, 'new Uri($"{normalizedBaseUrl}/", UriKind.Absolute)', "base trailing slash normalization")

    assert_contains(diagnostics, "BackendConstants.RootHealthEndpoint", "diagnostics primary root health")
    assert_contains(diagnostics, "BackendConstants.HealthEndpoint", "diagnostics api health fallback is not primary")
    if diagnostics.find("BackendConstants.HealthEndpoint") < diagnostics.find("BackendConstants.RootHealthEndpoint"):
        raise AssertionError("/api/health must not be checked before /health.")
    assert_contains(lesson_backend, "BackendConstants.RootHealthEndpoint", "lesson health preflight uses root health")
    assert_not_contains(lesson_backend, "BackendConstants.HealthEndpoint), cancellationToken)", "lesson health preflight using /api/health")
    assert_contains(lesson_backend, 'HealthyStatus = "Healthy"', "desktop health parser expects Healthy")

    for text, label in [(constants, "constants"), (settings_vm, "settings vm")]:
        for forbidden in ["/api/account/status", "/api/user/settings", '"/api/settings"']:
            assert_not_contains(text, forbidden, f"nonexistent endpoint in {label}")

    assert_contains(storage, 'BackendRequestDiagnosticsFileName = "backend-request-diagnostics.log"', "diagnostics log filename")
    for needle in [
        "TimestampUtc",
        "RequestName",
        "Method",
        "AbsoluteUrl",
        "HttpStatusCode",
        "ExceptionType",
        "SafeResponseBodySnippet",
        "EffectiveBackendBaseUrl",
        "BackendBaseUrlSource",
        "packaged default",
        "developer override",
        "packaged production server",
        "Bearer {RedactedText}",
        "EmailPattern",
    ]:
        assert_contains(request_diag, needle, f"diagnostic field/sanitizer {needle}")

    for text, label in [
        (diagnostics, "health diagnostics"),
        (auth, "auth diagnostics"),
        (settings_client, "settings diagnostics"),
        (subscription, "subscription diagnostics"),
    ]:
        assert_contains(text, "BackendRequestDiagnosticsService.RecordAsync", f"{label} logging")
        assert_contains(text, "BuildEndpointUri", f"{label} absolute URL builder")

    for request_name in [
        "backend_health",
        "database_health",
        "auth_register",
        "auth_login",
        "auth_me",
        "cloud_settings_get",
        "subscription_status",
    ]:
        combined = "\n".join([diagnostics, auth, settings_client, subscription])
        assert_contains(combined, request_name, f"logged request name {request_name}")

    assert_contains(settings_vm, "Backend request diagnostics log", "internal diagnostics report tracks log path")
    assert_contains(settings_vm, "BackendRequestDiagnosticsService.ReadReportAsync", "internal copy/export includes request diagnostics")
    assert_not_contains(read("Views/SettingsView.xaml"), "DiagnosticsSection", "release Settings UI Diagnostics tab")
    assert_not_contains(read("Views/SettingsView.xaml"), "BackendBaseUrl", "release Settings UI backend URL field")
    assert_contains(settings_vm, "StatusMessage = BackendUxText.SignedIn;", "post-auth optional failures do not overwrite auth success")

    for text, label in [(settings_vm, "SettingsViewModel"), (auth, "AuthBackendService"), (diagnostics, "BackendDiagnosticsService")]:
        for forbidden in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
            assert_not_contains(text, forbidden, f"blocking async in {label}")

    assert_contains(smoke, 'GET -Path "/health"', "smoke root health")
    assert_contains(smoke, 'GET -Path "/api/health/database"', "smoke database health")
    assert_contains(smoke, 'POST -Path "/api/auth/register"', "smoke auth register")
    assert_contains(smoke, 'GET -Path "/api/auth/me"', "smoke auth me")

    print("Desktop backend route/diagnostics policy checks passed.")


if __name__ == "__main__":
    main()
