#!/usr/bin/env python3
"""Policy checks for desktop update manifest loading diagnostics."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_URL = "https://languagevoicetutor.com/releases/windows/direct/latest.json"
PROD_BACKEND_URL = "https://api.languagevoicetutor.com"
VERSION_FILE = "release-version.txt"


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
    manifest_client = read("Services/Updates/UpdateManifestClient.cs")
    update_result = read("Services/Updates/UpdateCheckResult.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    startup_service = read("Services/Updates/DesktopStartupUpdateCheckService.cs")
    version_provider = read("Services/Updates/DesktopAppVersionProvider.cs")
    package_script = read("scripts/package-windows-inno-release.ps1")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_view_code = read("Views/SettingsView.xaml.cs")
    project = read("EnglishVoiceTutor.Desktop.csproj")
    backend_builder = read("Services/BackendEndpointBuilder.cs")

    assert_contains(manifest_client, f'LatestManifestUrl = "{MANIFEST_URL}"', "single public static latest.json URL")
    assert_not_contains(manifest_client, PROD_BACKEND_URL, "manifest client backend URL dependency")
    assert_not_contains(manifest_client, "BackendBaseUrl", "manifest client backend base URL dependency")
    assert_not_contains(manifest_client, "BuildEndpointUri", "manifest client backend endpoint builder")
    assert_contains(manifest_client, "new HttpRequestMessage(HttpMethod.Get, manifestUri)", "manifest URL used directly")
    assert_contains(manifest_client, "Uri.UriSchemeHttps", "HTTPS manifest access")
    assert_contains(manifest_client, "AutomaticDecompression", "normal compressed HTTPS response handling")
    assert_contains(manifest_client, "MediaTypeWithQualityHeaderValue(\"application/json\")", "JSON accept header")
    assert_contains(manifest_client, "ProductInfoHeaderValue", "safe app user agent")
    assert_contains(manifest_client, "ManifestRequestTimeoutSeconds = 45", "manifest timeout is not too short")
    assert_contains(manifest_client, "CacheControlHeaderValue", "valid typed no-cache headers")
    assert_contains(manifest_client, "request.Headers.Pragma.ParseAdd(\"no-cache\")", "valid pragma no-cache header")
    assert_contains(manifest_client, "JsonSerializer.DeserializeAsync<UpdateManifest>", "manifest JSON read and parse")

    for expected in ["ManifestUrl", "FailureCategory", "HttpStatusCode", "ExceptionMessage"]:
        assert_contains(update_result, expected, f"structured manifest load result {expected}")

    for expected in [
        "Private tester diagnostics:",
        "Manifest URL:",
        "Failure category:",
        "HTTP status:",
        "Details:",
        "BuildManualUpdateFailureMessage(result)",
    ]:
        assert_contains(settings_vm, expected, f"manual diagnostic message includes {expected}")
    assert_contains(settings_vm, "Could not check for updates right now. Please check your internet connection and try again.", "friendly manual fallback remains")
    manual_diagnostics = settings_vm.split("private static string BuildManualUpdateFailureMessage", 1)[1].split("private async Task DownloadVerifyAndMaybeRunUpdateAsync", 1)[0]
    assert_not_contains(manual_diagnostics.lower(), "password", "manual update diagnostics do not expose password labels")
    assert_not_contains(manual_diagnostics.lower(), "access token", "manual update diagnostics do not expose tokens")

    assert_contains(startup_service, "LoadLatestAsync", "background uses same manifest client")
    assert_contains(settings_vm, "LoadLatestAsync", "manual uses same manifest client")
    assert_contains(startup_service, "if (!result.IsSuccess || result.ValidationResult?.Manifest is null || result.ValidationResult.InstallerUri is null)\n            {\n                return;\n            }", "background manifest failure stays silent")
    assert_not_contains(startup_service, "Private tester diagnostics", "background does not show diagnostics")
    assert_not_contains(startup_service, "Could not check for updates right now", "background network failure stays silent")

    assert_contains(version_provider, f'BundledVersionFileName = "{VERSION_FILE}"', "release-version file name unchanged")
    assert_contains(version_provider, "Path.Combine(AppContext.BaseDirectory, BundledVersionFileName)", "release-version is read from app base directory")
    assert_contains(package_script, f'$bundledVersionFileName = "{VERSION_FILE}"', "packaging still writes release-version")
    assert_contains(package_script, "Set-Content -Path $bundledVersionFilePath -Value $Version -Encoding ascii -NoNewline", "packaging writes exact release version")

    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "DiagnosticsSectionNav", "release Settings Diagnostics nav")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Settings Backend URL binding")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL field")
    assert_contains(settings_view_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics UI disabled")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_BACKEND_URL}'", "release backend locked to production")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend remains server-only")

    release_builder = backend_builder.split("#else", 1)[1]
    assert_not_contains(release_builder.lower(), "localhost", "no localhost release backend behavior")
    assert_not_contains(startup_service.lower(), "localhost", "no localhost update behavior")
    assert_not_contains(manifest_client.lower(), "localhost", "no localhost manifest behavior")

    print("Desktop update manifest loading policy checks passed.")


if __name__ == "__main__":
    main()
