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

    assert_contains(settings_vm, "App updates", "visible update section")
    assert_contains(settings_xaml, "Check for updates", "manual check button")
    assert_contains(settings_xaml, "Download update", "manual download button")
    assert_contains(settings_xaml, "Open installer", "post-verification installer button")
    assert_contains(manifest_client, "https://languagevoicetutor.com/releases/windows/direct/latest.json", "latest.json reference")

    for field in [
        "ProductName",
        "AppId",
        "Platform",
        "Architecture",
        "Channel",
        "Version",
        "InstallerFileName",
        "InstallerRelativeUrl",
        "InstallerSha256",
        "InstallerSizeBytes",
        "BackendBaseUrl",
        "UpdateMode",
        "Notes",
    ]:
        assert_contains(manifest_client + read("Models/Updates/UpdateManifest.cs"), field, f"manifest field {field}")

    for expected in ["ExpectedProductName", "ExpectedAppId", "ExpectedPlatform", "ExpectedArchitecture"]:
        assert_contains(manifest_client, expected, f"manifest validation {expected}")
    assert_contains(manifest_client, "Uri.UriSchemeHttps", "HTTPS-only manifest and installer URLs")

    for version in ["0.1.17-tester.1", "0.1.18-tester.1"]:
        assert_contains(version_comparer, "ComparePrerelease", f"tester version comparison support for {version}")
    assert_contains(version_comparer, "GetChannel", "update channel detection")

    assert_contains(download_service, "SHA256.HashDataAsync", "SHA-256 verification")
    assert_contains(download_service, "File.Delete", "delete unsafe download")
    assert_contains(download_service, "OpenInstaller", "explicit installer open method")
    assert_not_contains(download_service, " /S", "silent installer switch")
    assert_not_contains(download_service, "--silent", "silent installer switch")
    assert_not_contains(download_service, "/VERYSILENT", "silent installer switch")

    update_flow = settings_vm + manifest_client + download_service
    for blocking in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
        assert_not_contains(update_flow, blocking, "blocking async pattern in update flow")

    assert_contains(settings_vm, "DownloadAndVerifyAsync", "download only after explicit command")
    assert_contains(settings_vm, "Installer downloaded and verified", "offer only after verification")
    assert_contains(settings_vm, "Do not download or open an installer during an active lesson", "active lesson safety note")


if __name__ == "__main__":
    main()
