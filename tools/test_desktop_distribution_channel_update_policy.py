#!/usr/bin/env python3
"""Policy checks for Direct vs Store desktop update behavior."""
from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_URL = "https://languagevoicetutor.com/releases/windows/direct/latest.json"
PROD_URL = "https://api.languagevoicetutor.com"
STORE_MESSAGE = "Updates are managed by Microsoft Store."


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
    project = read("EnglishVoiceTutor.Desktop.csproj")
    channel = read("Services/Updates/DesktopDistributionChannel.cs")
    policy = read("Services/Updates/DesktopUpdatePolicy.cs")
    manifest_client = read("Services/Updates/UpdateManifestClient.cs")
    startup = read("Services/Updates/DesktopStartupUpdateCheckService.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    download = read("Services/Updates/UpdateDownloadService.cs")
    backend_builder = read("Services/BackendEndpointBuilder.cs")

    assert_contains(project, "<DesktopDistributionChannel Condition=\"'$(DesktopDistributionChannel)' == ''\">Direct</DesktopDistributionChannel>", "Direct default distribution channel")
    assert_contains(project, "'$(DesktopDistributionChannel)' != 'Direct' and '$(DesktopDistributionChannel)' != 'Store'", "invalid channel build rejection")
    assert_contains(project, "DesktopDistributionChannel</_Parameter1>", "channel assembly metadata")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend URL lock")

    assert_contains(channel, "public enum DesktopDistributionChannel", "explicit channel enum")
    assert_contains(channel, "Direct", "Direct channel value")
    assert_contains(channel, "Store", "Store channel value")
    assert_contains(channel, "Unsupported desktop distribution channel", "runtime invalid channel fail-safe")

    assert_contains(policy, "CanUseDirectUpdateManifest => DesktopDistributionChannelProvider.IsDirect", "single manifest boundary")
    assert_contains(policy, "CanDownloadDirectInstaller => DesktopDistributionChannelProvider.IsDirect", "single installer download boundary")
    assert_contains(policy, "CanLaunchDirectInstaller => DesktopDistributionChannelProvider.IsDirect", "single installer launch boundary")
    assert_contains(policy, STORE_MESSAGE, "user-facing Microsoft Store update message")

    assert_contains(manifest_client, MANIFEST_URL, "Direct latest.json URL remains unchanged")
    assert_contains(startup, "if (!DesktopUpdatePolicy.ShouldRunStartupDirectUpdateCheck)", "Store startup avoids direct manifest path")
    assert_contains(startup, "var result = await updateManifestClient.LoadLatestAsync();", "Direct startup still loads manifest")
    assert_contains(settings_vm, "if (!DesktopUpdatePolicy.CanUseDirectUpdateManifest)", "Store manual check avoids manifest")
    assert_contains(settings_vm, "ShowUpdateMessage(LocalizeUiText(DesktopUpdatePolicy.StoreManagedUpdatesMessage)", "Store manual check shows Store message")
    assert_contains(settings_vm, "var result = await updateManifestClient.LoadLatestAsync();", "Direct manual check still loads manifest")
    assert_contains(settings_vm, "await DownloadVerifyAndMaybeRunUpdateAsync(latestUpdateManifest, latestInstallerUri);", "Direct manual download flow preserved")
    assert_contains(download, "if (!DesktopUpdatePolicy.CanDownloadDirectInstaller)", "Store cannot download direct installer")
    assert_contains(download, "if (!DesktopUpdatePolicy.CanLaunchDirectInstaller)", "Store cannot launch direct installer")
    assert_contains(download, "safeFileName.EndsWith(\".exe\"", "Direct installer remains .exe verified flow")
    assert_contains(download, "StartDetachedDelayedInstallerLauncher", "Direct installer launch remains available")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend remains production locked")

    forbidden_secret_markers = ["sk-", "pdl_", "BEGIN PRIVATE KEY", "Password="]
    for relative in [
        "Services/Updates/DesktopDistributionChannel.cs",
        "Services/Updates/DesktopUpdatePolicy.cs",
        "Services/Updates/DesktopStartupUpdateCheckService.cs",
        "ViewModels/SettingsViewModel.cs",
        "Services/Updates/UpdateDownloadService.cs",
        "EnglishVoiceTutor.Desktop.csproj",
    ]:
        text = read(relative)
        for marker in forbidden_secret_markers:
            assert_not_contains(text, marker, f"secret marker {marker} in {relative}")

    print("Desktop distribution channel update policy checks passed.")


if __name__ == "__main__":
    main()
