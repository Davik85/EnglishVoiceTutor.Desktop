#!/usr/bin/env python3
"""Policy checks for the safe startup background update flow."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
UPDATE_MESSAGE = "A new version of Language Voice Tutor is available. Do you want to download and install it now?"
INSTALLER_MESSAGE = "The update was downloaded and verified. Do you want to start the installer now?"
LATEST_VERSION_MESSAGE = "You are using the latest version."


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
    main_window = read("MainWindow.xaml.cs")
    startup_service = read("Services/Updates/DesktopStartupUpdateCheckService.cs")
    manifest_client = read("Services/Updates/UpdateManifestClient.cs")
    download_service = read("Services/Updates/UpdateDownloadService.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_view_code = read("Views/SettingsView.xaml.cs")
    project = read("EnglishVoiceTutor.Desktop.csproj")
    backend_builder = read("Services/BackendEndpointBuilder.cs")

    assert_contains(main_window, "OnContentRendered", "UI-ready startup hook")
    assert_contains(main_window, "StartOnceWhenUiIsReady(this, IsLessonActive)", "background update check launched once after UI is ready")
    assert_contains(startup_service, "private bool hasStarted", "at-most-once process guard")
    assert_contains(startup_service, "Task.Delay(TimeSpan.FromSeconds(StartupUpdateCheckDelaySeconds))", "startup/session-restore grace delay")
    assert_contains(startup_service, "LoadLatestAsync", "background flow uses manifest client")
    assert_contains(manifest_client, "https://languagevoicetutor.com/releases/windows/direct/latest.json", "same latest.json flow")
    assert_contains(startup_service, "UpdateVersionComparer.Compare", "background version comparison")
    assert_contains(startup_service, "DownloadAndVerifyAsync", "background flow uses existing download and verification service")
    assert_contains(download_service, "SHA256.HashDataAsync", "trusted SHA-256 verification remains in shared service")
    assert_contains(startup_service, "UpdateDownloadService.OpenInstaller(result.FilePath)", "background flow uses shared installer launch")

    assert_contains(startup_service, UPDATE_MESSAGE, "first update confirmation")
    assert_contains(startup_service, INSTALLER_MESSAGE, "second installer confirmation")
    assert_contains(startup_service, "MessageBoxButton.YesNo", "Yes/No confirmations")
    assert_contains(startup_service, "downloadChoice != MessageBoxResult.Yes", "No before download does nothing")
    assert_contains(startup_service, "installChoice == MessageBoxResult.Yes", "installer starts only after second Yes")
    assert_contains(startup_service, "Please finish your current lesson before starting the installer.", "active lesson friendly installer block")
    assert_contains(startup_service, "catch (Exception exception)", "fire-and-forget failure containment")
    assert_contains(startup_service, "return;", "silent no-update/failure returns")
    assert_not_contains(startup_service, LATEST_VERSION_MESSAGE, "background no-update latest-version dialog")

    assert_contains(settings_xaml, "CheckForUpdatesCommand", "manual Settings update button remains available")
    assert_contains(settings_vm, LATEST_VERSION_MESSAGE, "manual update flow may still show latest-version dialog")
    assert_contains(settings_vm, UPDATE_MESSAGE, "manual flow update available confirmation remains")
    assert_contains(settings_vm, INSTALLER_MESSAGE, "manual flow second installer confirmation remains")

    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "DiagnosticsSectionNav", "release Settings Diagnostics nav")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Settings Backend URL binding")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL label")
    assert_contains(settings_view_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics UI remains disabled")

    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend build lock")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend overrides ignored")
    assert_not_contains(startup_service.lower(), "localhost", "no local backend behavior in background update check")
    assert_not_contains(startup_service, "DesktopBackendBaseUrl", "no backend override in background update check")

    for combined, label in [(startup_service + settings_vm + download_service, "update flow"), (project, "project")]:
        for forbidden in ["--silent", "/VERYSILENT", "/SILENT", " /S"]:
            assert_not_contains(combined, forbidden, f"silent installer switch in {label}")

    print("Desktop background update check policy checks passed.")


if __name__ == "__main__":
    main()
