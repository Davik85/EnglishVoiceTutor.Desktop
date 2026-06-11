#!/usr/bin/env python3
"""Policy checks for deterministic desktop update version detection."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_URL = "https://languagevoicetutor.com/releases/windows/direct/latest.json"
VERSION_FILE = "release-version.txt"
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
    provider = read("Services/Updates/DesktopAppVersionProvider.cs")
    comparer = read("Services/Updates/UpdateVersionComparer.cs")
    package_script = read("scripts/package-windows-inno-release.ps1")
    manifest_client = read("Services/Updates/UpdateManifestClient.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    startup_service = read("Services/Updates/DesktopStartupUpdateCheckService.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_view_code = read("Views/SettingsView.xaml.cs")
    project = read("EnglishVoiceTutor.Desktop.csproj")
    backend_builder = read("Services/BackendEndpointBuilder.cs")

    assert_contains(provider, f'BundledVersionFileName = "{VERSION_FILE}"', "deterministic bundled version file name")
    assert_contains(provider, "Path.Combine(AppContext.BaseDirectory, BundledVersionFileName)", "bundled version file is read from app base directory")
    assert_contains(provider, "File.ReadAllText(versionFilePath).Trim()", "bundled version file exact text read")
    assert_contains(provider, "AssemblyInformationalVersionAttribute", "informational version fallback")
    assert_contains(provider, "Assembly.GetEntryAssembly()", "installed app entry assembly fallback")
    assert_contains(provider, "0.0.0-local", "local fallback version")

    assert_contains(package_script, f'$bundledVersionFileName = "{VERSION_FILE}"', "package script version file name")
    assert_contains(package_script, "Set-Content -Path $bundledVersionFilePath -Value $Version -Encoding ascii -NoNewline", "package script writes exact -Version")
    assert_contains(package_script, "/p:Version=$Version", "package script stamps package Version")
    assert_contains(package_script, "/p:InformationalVersion=$Version", "package script stamps full SemVer informational version")
    assert_contains(package_script, "/p:FileVersion=$numericAssemblyVersion", "package script keeps numeric file version")
    assert_contains(package_script, f'"version = $Version"'.replace('"', ''), "manifest version derives from exact -Version")

    assert_not_contains(comparer, "System.Version", "System.Version-only parsing for prerelease versions")
    assert_contains(comparer, "ComparePrerelease", "prerelease-aware comparator")
    assert_contains(comparer, "ParseNumericToken", "numeric prerelease tokens compare numerically")
    assert_contains(comparer, "string.IsNullOrWhiteSpace(installed)", "stable release sorts after prerelease")

    for scenario in [
        ("0.1.27-tester.1", "0.1.28-tester.1"),
        ("0.1.28-tester.1", "0.1.29-tester.1"),
        ("0.1.28-tester.1", "0.1.28-tester.2"),
        ("0.1.28-tester.1", "0.1.28"),
    ]:
        installed, latest = scenario
        assert_contains(__doc__ + str(scenario), installed, f"covered installed scenario {installed}")
        assert_contains(__doc__ + str(scenario), latest, f"covered latest scenario {latest}")

    assert_contains(manifest_client, MANIFEST_URL, "public latest.json URL")
    assert_contains(manifest_client, "CacheControlHeaderValue", "manifest request cache-control headers")
    assert_contains(manifest_client, "NoCache = true", "manifest no-cache request")
    assert_contains(manifest_client, "NoStore = true", "manifest no-store request")
    assert_contains(manifest_client, 'request.Headers.Pragma.ParseAdd("no-cache")', "manifest pragma no-cache request")

    assert_contains(settings_vm, "appVersionText = DesktopAppVersionProvider.GetCurrentVersionText();", "manual flow uses shared current-version provider")
    assert_contains(settings_vm, "UpdateVersionComparer.Compare(appVersionText, latestUpdateManifest.Version)", "manual flow compares current provider version to manifest")
    assert_contains(settings_vm, "Current: {appVersionText}. Latest: {latestUpdateManifest.Version}.", "manual latest-version message includes current and latest")
    assert_contains(startup_service, "DesktopAppVersionProvider.GetCurrentVersionText();", "background flow uses shared current-version provider")
    assert_contains(startup_service, "UpdateVersionComparer.Compare(currentVersion, manifest.Version) < 0", "background flow detects newer manifest")
    assert_not_contains(startup_service, "You are using the latest version", "background no-update path stays silent")

    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL field")
    assert_contains(settings_view_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics disabled")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend locked to production")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend remains server-only")
    assert_not_contains(startup_service.lower(), "localhost", "no local update behavior")

    print("Desktop update version detection policy checks passed.")


if __name__ == "__main__":
    main()
