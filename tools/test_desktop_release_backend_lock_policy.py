#!/usr/bin/env python3
"""Policy checks that non-Debug desktop builds are server-only."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
FORBIDDEN_RELEASE_BACKENDS = ["http://localhost:5000", "127.0.0.1", "localhost"]


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


def assert_not_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, re.S):
        raise AssertionError(f"Forbidden {label}: {pattern}")


def main() -> None:
    project = read("EnglishVoiceTutor.Desktop.csproj")
    constants = read("Constants/BackendConstants.cs")
    builder = read("Services/BackendEndpointBuilder.cs")
    user_settings = read("Models/UserSettings.cs")
    settings_service = read("Services/UserSettingsService.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_view_code = read("Views/SettingsView.xaml.cs")
    main_vm = read("ViewModels/MainViewModel.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    auth = read("Services/Auth/AuthBackendService.cs")
    lesson_chat = read("Services/LessonChatBackendService.cs")
    lesson_session = read("Services/BackendLessonSessionClient.cs")
    lesson_message = read("Services/BackendLessonMessageClient.cs")
    lesson_summary = read("Services/BackendLessonSummaryClient.cs")
    package_script = read("scripts/package-windows-inno-release.ps1")
    docs = "\n".join(read(path) for path in [
        "docs/CURRENT_STATE.md",
        "docs/NEXT_STEPS.md",
        "docs/TESTER_RELEASE.md",
        "docs/WINDOWS_RELEASE_SERVER_UPLOAD.md",
    ])

    assert_contains(constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "single production backend constant")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend override build failure")
    assert_contains(package_script, "Tester/release installed builds are server-only", "packaging rejects custom release backends")
    assert_contains(package_script, "$BackendBaseUrl -ne $productionBackendBaseUrl", "packaging locks backend URL")
    assert_contains(package_script, "Release publish output contains forbidden backend override/UI string", "publish output forbidden backend scan")
    assert_contains(project, "'$(Configuration)' == 'Debug'", "localhost is Debug-only MSBuild configuration")
    assert_contains(constants, "#if DEBUG", "developer backend constant is compile-time Debug-only")
    assert_contains(builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release NormalizeBaseUrl ignores input")
    assert_contains(builder, "ResolveSavedBaseUrlForCurrentBuild", "stale saved backend resolver")
    assert_contains(builder, "return BackendConstants.ProductionBackendBaseUrl;", "production fallback")

    release_section = builder.split("#else", 1)[1]
    for forbidden in FORBIDDEN_RELEASE_BACKENDS:
        assert_not_contains(release_section, forbidden, "release endpoint builder local backend")

    assert_contains(user_settings, "[JsonIgnore]", "release settings do not save backend URL")
    assert_contains(user_settings, "#if !DEBUG", "backend URL JsonIgnore is release-only")
    assert_contains(settings_service, "ResolveSavedBaseUrlForCurrentBuild(settings.BackendBaseUrl)", "loaded stale backend URLs are resolved for current build")
    assert_contains(main_vm, "ResolveSavedBaseUrlForCurrentBuild", "main view model never applies raw saved backend URL")

    assert_not_contains(settings_xaml, "BackendBaseUrl", "release settings XAML backend URL binding")
    assert_not_contains(settings_xaml, "Backend URL", "release settings XAML backend URL label")
    assert_not_contains(settings_xaml, "DiagnosticsSection", "release settings Diagnostics section")
    assert_not_contains(settings_xaml, "DiagnosticsSectionNav", "release settings Diagnostics tab")
    assert_contains(settings_view_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics UI disabled")

    for source, label in [
        (auth, "auth"),
        (lesson_chat, "lesson chat"),
        (lesson_session, "lesson session"),
        (lesson_message, "lesson message"),
        (lesson_summary, "lesson summary"),
    ]:
        assert_contains(source, "BuildEndpointUri", f"{label} uses shared backend endpoint builder")

    assert_contains(auth, "AuthRegisterEndpoint", "auth register uses configured route")
    assert_contains(auth, "AuthLoginEndpoint", "auth login uses configured route")
    assert_contains(auth, "AuthMeEndpoint", "auth me uses configured route")
    assert_contains(lesson_chat, "RootHealthEndpoint", "connectivity uses root health")

    combined_backend_path = "\n".join([main_vm, settings_vm, auth, lesson_chat, lesson_session, lesson_message, lesson_summary])
    for forbidden in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
        assert_not_contains(combined_backend_path, forbidden, "blocking async pattern")

    for expected in [
        "Release/tester installed builds are server-only",
        PROD_URL,
        "Local backend URLs are DEBUG/developer-only",
        "Diagnostics and Backend URL editing are not part of user/release Settings",
        "registration/login/lesson/history/progress/update",
    ]:
        assert_contains(docs, expected, f"server-only release docs: {expected}")

    print("Desktop release backend lock policy checks passed.")


if __name__ == "__main__":
    main()
