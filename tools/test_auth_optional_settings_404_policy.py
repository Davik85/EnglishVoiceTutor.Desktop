#!/usr/bin/env python3
"""Static policy checks for auth success with optional settings/account-status 404 fallback."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    constants = read("Constants/BackendConstants.cs")
    backend_constants = read("backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs")
    backend_program = read("backend/EnglishVoiceTutor.Api/Program.cs")
    subscription_endpoints = read("backend/EnglishVoiceTutor.Api/Endpoints/SubscriptionStatusEndpoints.cs")
    auth_backend = read("Services/Auth/AuthBackendService.cs")

    assert_contains(constants, 'ProductionBackendBaseUrl = "https://api.languagevoicetutor.com"', "production backend default")
    assert_contains(constants, 'MeSettingsEndpoint = "/api/me/settings"', "desktop authenticated settings route")
    assert_contains(backend_constants, 'MeUserSettingsRoute = "/api/me/settings"', "backend authenticated settings route")
    assert_contains(backend_program, "app.MapGet(ApiConstants.MeUserSettingsRoute", "backend maps authenticated settings GET")
    assert_contains(backend_program, "app.MapPut(ApiConstants.MeUserSettingsRoute", "backend maps authenticated settings PUT")
    assert_contains(constants, 'MeSubscriptionStatusEndpoint = "/api/me/subscription-status"', "desktop account/subscription status route")
    assert_contains(subscription_endpoints, "ApiConstants.MeSubscriptionStatusRoute", "backend maps authenticated subscription status")

    for forbidden in ["/api/account/status", "/api/user/settings", '"/api/settings"']:
        assert_not_contains(constants, forbidden, "nonexistent desktop endpoint")
        assert_not_contains(settings_vm, forbidden, "nonexistent settings view model endpoint")

    assert_contains(auth_backend, "await sessionStorageService.SaveAsync(storedSession", "auth persists session immediately on auth success")
    assert_contains(settings_vm, "RunPostAuthRefreshesAsync", "post-auth optional refresh isolation")
    assert_contains(settings_vm, "await RunPostAuthRefreshesAsync();", "registration/login use isolated post-auth refresh")
    assert_contains(settings_vm, "IsOptionalEndpointMissing", "optional 404 classifier")
    assert_contains(settings_vm, "statusCode == HttpStatusCode.NotFound", "404 is optional endpoint missing")
    assert_contains(settings_vm, "Cloud settings are not available yet. Local settings are still available.", "safe cloud settings 404 message")
    assert_contains(settings_vm, "missing optional settings endpoint", "settings diagnostics distinguish optional 404")
    assert_contains(settings_vm, "missing optional account status endpoint", "account status diagnostics distinguish optional 404")
    assert_contains(settings_vm, "Backend settings endpoint", "diagnostics include settings endpoint status")
    assert_contains(settings_vm, "Account status endpoint", "diagnostics include account status endpoint status")
    assert_contains(settings_vm, "BackendUxText.SignedIn", "auth success remains signed in after optional refresh")

    post_auth = re.search(r"private async Task RunPostAuthRefreshesAsync\(\).*?\n    private async Task RefreshBackendHealthDiagnosticsAsync", settings_vm, re.S)
    if not post_auth:
        raise AssertionError("Could not locate RunPostAuthRefreshesAsync.")
    post_auth_body = post_auth.group(0)
    for expected in ["LoadSettingsForCurrentSessionAsync", "RefreshLearningStatisticsAsync", "RefreshSubscriptionStatusAsync"]:
        assert_contains(post_auth_body, expected, f"post-auth {expected}")
    assert_contains(post_auth_body, "catch", "post-auth optional refresh catches failures")
    assert_not_contains(post_auth_body, "ErrorMessage = BackendUxText.CouldNotConnect", "post-auth optional refresh must not convert auth success to auth failure")

    for text, label in [(settings_vm, "SettingsViewModel"), (auth_backend, "AuthBackendService")]:
        for forbidden in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
            assert_not_contains(text, forbidden, f"blocking async in {label}")

    print("Auth optional settings/account-status 404 fallback policy checks passed.")


if __name__ == "__main__":
    main()
