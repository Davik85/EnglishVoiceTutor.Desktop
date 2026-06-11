#!/usr/bin/env python3
"""Policy checks for the in-app update installer launch UX."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
INSTALLER_READY_MESSAGE = (
    "The update was downloaded and verified. Language Voice Tutor will close and restart "
    "during installation. Do you want to start the installer now?"
)
LAUNCH_HELPER = "TryStartVerifiedInstallerAfterAppShutdown"


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


def assert_ordered(text: str, needles: list[str], label: str) -> None:
    position = -1
    for needle in needles:
        next_position = text.find(needle, position + 1)
        if next_position == -1:
            raise AssertionError(f"Missing ordered step for {label}: {needle}")
        position = next_position


def main() -> None:
    startup_service = read("Services/Updates/DesktopStartupUpdateCheckService.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    download_service = read("Services/Updates/UpdateDownloadService.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_view_code = read("Views/SettingsView.xaml.cs")
    project = read("EnglishVoiceTutor.Desktop.csproj")
    backend_builder = read("Services/BackendEndpointBuilder.cs")
    installer_script = read("installer/windows/LanguageVoiceTutor.iss")

    for source, label in [(startup_service, "background update flow"), (settings_vm, "manual Settings update flow")]:
        assert_contains(source, INSTALLER_READY_MESSAGE, f"clear second confirmation in {label}")
        assert_contains(source, "MessageBoxButton.YesNo", f"Yes/No confirmation in {label}")
        assert_contains(source, "installChoice == MessageBoxResult.Yes", f"installer launch requires explicit Yes in {label}")
        assert_contains(source, f"UpdateDownloadService.{LAUNCH_HELPER}(result.FilePath", f"shared launch helper in {label}")
        assert_ordered(source, ["DownloadAndVerifyAsync", "result.IsSuccess", "installChoice == MessageBoxResult.Yes", LAUNCH_HELPER], f"verified-confirmed-launch sequence in {label}")

    assert_contains(download_service, f"public static bool {LAUNCH_HELPER}", "shared installer launch helper")
    assert_contains(download_service, "StartDetachedDelayedInstallerLauncher", "external detached helper launcher")
    assert_contains(download_service, 'Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"', "cmd.exe helper process")
    assert_contains(download_service, "timeout /t", "delayed installer start")
    assert_contains(download_service, "BeginApplicationShutdown", "app shutdown request")
    assert_contains(download_service, "application.Dispatcher.BeginInvoke", "non-blocking shutdown dispatch")
    assert_contains(download_service, "showStartFailure?.Invoke", "friendly installer launch failure message")
    assert_contains(download_service, "SHA256.HashDataAsync", "mandatory SHA-256 verification remains")
    assert_contains(download_service, "The downloaded installer did not pass verification. It was deleted for safety.", "failed SHA-256 blocks launch")

    combined_update_code = startup_service + settings_vm + download_service + project + installer_script
    for forbidden in ["--silent", "/VERYSILENT", "/SILENT", "/SUPPRESSMSGBOXES", " /S"]:
        assert_not_contains(combined_update_code, forbidden, "silent or wizard-skipping installer option")

    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "DiagnosticsSectionNav", "release Settings Diagnostics nav")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Settings Backend URL binding")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL label")
    assert_contains(settings_view_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release Diagnostics disabled")

    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend build lock")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend ignores overrides")
    assert_not_contains(startup_service.lower(), "localhost", "no local backend behavior in background update flow")

    print("Desktop update installer launch UX policy checks passed.")


if __name__ == "__main__":
    main()
