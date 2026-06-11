#!/usr/bin/env python3
"""Static policy checks for the desktop welcome-screen account entrypoint."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]

FILES = {
    "welcome_xaml": ROOT / "Views" / "WelcomeView.xaml",
    "welcome_vm": ROOT / "ViewModels" / "WelcomeViewModel.cs",
    "main_vm": ROOT / "ViewModels" / "MainViewModel.cs",
    "settings_xaml": ROOT / "Views" / "SettingsView.xaml",
    "settings_vm": ROOT / "ViewModels" / "SettingsViewModel.cs",
    "auth_backend": ROOT / "Services" / "Auth" / "AuthBackendService.cs",
    "app_text": ROOT / "Localization" / "AppLocalizedText.cs",
    "app_localization": ROOT / "Localization" / "AppLocalization.cs",
    "project": ROOT / "EnglishVoiceTutor.Desktop.csproj",
    "endpoint_builder": ROOT / "Services" / "BackendEndpointBuilder.cs",
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


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.S):
        raise AssertionError(f"Missing {label}: {pattern}")


def main() -> None:
    welcome_xaml = read("welcome_xaml")
    welcome_vm = read("welcome_vm")
    main_vm = read("main_vm")
    settings_xaml = read("settings_xaml")
    settings_vm = read("settings_vm")
    auth_backend = read("auth_backend")
    app_text = read("app_text")
    app_localization = read("app_localization")
    project = read("project")
    endpoint_builder = read("endpoint_builder")

    assert_contains(welcome_xaml, "AccountStatusButtonText", "welcome account button binding")
    assert_contains(welcome_xaml, "OpenAccountSettingsCommand", "welcome account command binding")
    assert_regex(
        welcome_xaml,
        r"StartLessonCommand.*?OpenSettingsCommand.*?OpenAccountSettingsCommand",
        "welcome button order: start, settings, account",
    )

    assert_contains(app_text, "WelcomeSignInToAccountButton", "localized signed-out welcome string")
    assert_contains(app_text, "WelcomeSignedInAsFormat", "localized signed-in welcome format")
    assert_contains(app_text, "WelcomeSignedInButton", "localized signed-in fallback string")
    assert_contains(app_localization, 'l("Sign in to account")', "signed-out account entrypoint text")
    assert_contains(app_localization, 'l("Signed in as {0}")', "signed-in account entrypoint format")
    assert_contains(app_localization, 'l("Signed in")', "signed-in no-identity fallback text")

    assert_contains(welcome_vm, "AuthBackendService authBackendService", "welcome reuses auth service")
    assert_contains(welcome_vm, "TryRestoreSessionAsync", "welcome restores account state on startup")
    assert_contains(welcome_vm, "AuthStateChanged", "welcome observes auth state changes")
    assert_contains(welcome_vm, "user.DisplayName", "display name preferred")
    assert_contains(welcome_vm, "user.Email", "email fallback")
    assert_contains(auth_backend, "event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged", "auth state change event")
    assert_contains(auth_backend, "NotifyAuthStateChanged(payload.User)", "login/register notifies welcome")
    assert_contains(auth_backend, "NotifyAuthStateChanged(null)", "logout/invalid-session notifies welcome")

    assert_contains(main_vm, "SettingsSection.Account", "account settings target")
    assert_contains(
        main_vm,
        "() => NavigateToSettings(NavigateToWelcome, SettingsSection.Account)",
        "welcome account command opens settings account section",
    )
    assert_contains(settings_vm, "initialSection = SettingsSection.Learning", "settings keeps default learning section")
    assert_contains(settings_vm, "SettingsSection.Account", "settings supports account initial section")
    assert_contains(settings_xaml, "IsAccountSectionSelected", "account nav selection binding")
    assert_contains(settings_xaml, "AccountSectionNav", "existing account section remains")

    assert_not_contains(settings_xaml, "DiagnosticsSectionNav", "release diagnostics navigation tab")
    assert_not_contains(settings_xaml, "DiagnosticsSection", "release diagnostics section")
    assert_not_contains(settings_xaml, "BackendUrlLabel", "release Backend URL field")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Backend URL binding")

    release_default_line = re.search(
        r"<DesktopBackendBaseUrl[^>]*Configuration\)' != 'Debug'[^>]*>(.*?)</DesktopBackendBaseUrl>",
        project,
    )
    if not release_default_line or release_default_line.group(1).strip() != PROD_URL:
        raise AssertionError("Release backend default must remain the production server URL.")
    assert_contains(endpoint_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release endpoint lock")
    release_returns = re.findall(r"#else\s*(.*?)#endif", endpoint_builder, re.S)
    if not release_returns:
        raise AssertionError("Could not find release endpoint branches.")
    for release_region in release_returns:
        assert_not_contains(release_region.lower(), "localhost", "release localhost backend behavior")
        assert_not_contains(release_region, "DeveloperBackendBaseUrl", "release developer backend override")

    print("Desktop account entrypoint policy checks passed.")


if __name__ == "__main__":
    main()
