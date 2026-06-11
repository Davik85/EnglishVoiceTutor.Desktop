from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    settings_xaml = read("Views/SettingsView.xaml")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    manifest_client = read("Services/Updates/UpdateManifestClient.cs")
    version_comparer = read("Services/Updates/UpdateVersionComparer.cs")
    download_service = read("Services/Updates/UpdateDownloadService.cs")
    manifest_notes = read("docs/WINDOWS_INSTALLER_UPDATE_FLOW.md") + read("docs/TESTER_RELEASE.md") + read("scripts/package-windows-inno-release.ps1")

    learning_section = settings_xaml.split('<ScrollViewer x:Name="AccountSection"', 1)[0]
    diagnostics_split = settings_xaml.split('<ScrollViewer x:Name="DiagnosticsSection"', 1)
    diagnostics_section = diagnostics_split[-1] if len(diagnostics_split) > 1 else ""
    assert_contains(learning_section, "CheckForUpdatesCommand", "user-facing update button outside Diagnostics-only UI")
    assert_contains(settings_vm, "Check for updates", "user-facing update button text")
    assert_not_contains(diagnostics_section, "Check for updates", "Diagnostics-only update entry point")

    for forbidden in [
        "Download update",
        "Open folder",
        "Manifest:",
        "UpdateManifestUrlText",
        "UpdateInstallerSize",
        "UpdateChannel",
        "Installer size",
        "SHA-256",
        "latest.json details",
        "SmartScreen",
    ]:
        assert_not_contains(settings_xaml, forbidden, "technical update dashboard UI")

    assert_contains(settings_vm, "MessageBoxButton.YesNo", "update available Yes/No confirmation")
    assert_contains(settings_vm, "A new version of Language Voice Tutor is available", "simple update available message")
    assert_contains(settings_vm, "You are using the latest version.", "simple up-to-date dialog")
    assert_contains(settings_vm, "This app version is newer than the public update manifest.", "newer-than-manifest warning")
    assert_contains(settings_vm, "Could not check for updates right now. Please check your internet connection and try again.", "friendly manifest failure dialog")
    assert_contains(settings_vm, "The update was downloaded and verified. Do you want to start the installer now?", "post-verification installer confirmation")
    assert_contains(settings_vm, "DownloadAndVerifyAsync", "explicit download and verify step")
    assert_contains(settings_vm, "OpenInstaller(result.FilePath)", "installer opens only after verified result")

    assert_contains(manifest_client, "https://languagevoicetutor.com/releases/windows/direct/latest.json", "latest.json reference")
    for expected in ["ExpectedProductName", "ExpectedAppId", "ExpectedPlatform", "ExpectedArchitecture"]:
        assert_contains(manifest_client, expected, f"manifest validation {expected}")
    assert_contains(manifest_client, "Uri.UriSchemeHttps", "HTTPS-only manifest and installer URLs")
    assert_contains(version_comparer, "ComparePrerelease", "tester version comparison")

    assert_contains(download_service, "SHA256.HashDataAsync", "SHA-256 verification")
    assert_contains(download_service, "File.Delete", "delete unsafe download")
    assert_contains(download_service, "UseShellExecute = true", "normal ShellExecute installer launch")
    for forbidden in [" /S", "--silent", "/VERYSILENT", "/SILENT", "runas"]:
        assert_not_contains(download_service, forbidden, "silent/elevated installer switch")

    update_flow = settings_vm + manifest_client + download_service
    for blocking in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
        assert_not_contains(update_flow, blocking, "blocking async pattern in update flow")

    assert_not_contains(manifest_notes.lower(), "update ui not implemented yet", "stale manifest note")


if __name__ == "__main__":
    main()
