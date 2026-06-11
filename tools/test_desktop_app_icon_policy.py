#!/usr/bin/env python3
"""Policy checks for the desktop app and Windows installer icon wiring."""
from __future__ import annotations

import pathlib
import re
import struct

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
ICON_PATH = "Assets/Branding/app-icon.ico"
SOURCE_ICON_PATH = "Assets/Branding/app-icon-source.png"
BRANDING_PLACEHOLDER_PATH = "Assets/Branding/README.md"
EXPECTED_ICON_SIZES = {16, 24, 32, 48, 64, 128, 256}


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


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Missing {label}: {pattern}")


def read_ico_sizes(relative: str) -> set[int]:
    path = ROOT / relative
    if not path.exists():
        return set()

    data = path.read_bytes()
    if len(data) < 6:
        raise AssertionError(f"Icon file is too small: {relative}")

    reserved, icon_type, count = struct.unpack_from("<HHH", data, 0)
    if reserved != 0 or icon_type != 1:
        raise AssertionError(f"Icon file is not a Windows .ico: {relative}")

    sizes: set[int] = set()
    for index in range(count):
        offset = 6 + index * 16
        width, height = struct.unpack_from("<BB", data, offset)
        sizes.add(256 if width == 0 else width)
        if width != height:
            raise AssertionError(f"Icon entry {index} is not square: {width}x{height}")

    return sizes


def main() -> None:
    project = read("EnglishVoiceTutor.Desktop.csproj")
    main_window = read("MainWindow.xaml")
    main_window_code = read("MainWindow.xaml.cs")
    inno = read("installer/windows/LanguageVoiceTutor.iss")
    package_script = read("scripts/package-windows-inno-release.ps1")
    icon_script = read("scripts/generate-app-icon.ps1")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_code = read("Views/SettingsView.xaml.cs")
    backend_constants = read("Constants/BackendConstants.cs")
    backend_builder = read("Services/BackendEndpointBuilder.cs")
    placeholder = read(BRANDING_PLACEHOLDER_PATH)

    actual_sizes = read_ico_sizes(ICON_PATH)
    if actual_sizes:
        missing_sizes = EXPECTED_ICON_SIZES - actual_sizes
        if missing_sizes:
            raise AssertionError(f"{ICON_PATH} is missing required sizes: {sorted(missing_sizes)}")
    else:
        assert_contains(placeholder, SOURCE_ICON_PATH, "source icon placeholder instructions")
        assert_contains(placeholder, ICON_PATH, "generated icon placeholder instructions")
        assert_contains(placeholder, "Binary icon files are intentionally not committed", "binary placeholder policy")

    assert_contains(project, "<AppIconPath>Assets\\Branding\\app-icon.ico</AppIconPath>", "central app icon path")
    assert_contains(project, "<ApplicationIcon Condition=\"Exists('$(AppIconPath)')\">$(AppIconPath)</ApplicationIcon>", "conditional compiled executable icon")
    assert_contains(project, "<Resource Include=\"$(AppIconPath)\" Condition=\"Exists('$(AppIconPath)')\" />", "conditional WPF packaged icon resource")
    assert_contains(main_window_code, f'AppIconResourcePath = "{ICON_PATH}"', "main window icon path")
    assert_contains(main_window_code, "ApplyAppIconIfAvailable();", "main window applies icon when present")
    assert_contains(main_window_code, "Application.GetResourceStream(iconUri)", "main window loads packaged icon resource")
    assert_not_contains(main_window, f'Icon="{ICON_PATH}"', "unconditional missing XAML icon reference")

    assert_contains(inno, '#define AppIconFile "..\\..\\Assets\\Branding\\app-icon.ico"', "Inno icon constant")
    assert_contains(inno, "SetupIconFile={#AppIconFile}", "installer executable icon")
    assert_contains(inno, "UninstallDisplayIcon={#InstalledAppIconFile}", "installed app uninstall icon")
    assert_regex(inno, r'Source:\s+"\{#AppIconFile\}";\s+DestDir:\s+"\{app\}\\Assets\\Branding"', "installed icon file")
    assert_regex(inno, r'Name:\s+"\{group\}\\Language Voice Tutor".*IconFilename:\s+"\{#InstalledAppIconFile\}"', "Start Menu shortcut icon")
    assert_regex(inno, r'Name:\s+"\{commondesktop\}\\Language Voice Tutor".*IconFilename:\s+"\{#InstalledAppIconFile\}"', "common desktop shortcut icon")
    assert_contains(inno, "[InstallDelete]", "installer removes stale shortcuts before recreating them")
    assert_contains(inno, 'PrivilegesRequired=admin', "admin installer mode")
    assert_contains(inno, 'Type: files; Name: "{commondesktop}\\Language Voice Tutor.lnk"', "common desktop stale shortcut cleanup")
    assert_not_contains(inno.lower(), "{userdesktop}", "per-user desktop usage in admin installer")
    assert_not_contains(inno.lower(), "{autodesktop}", "automatic desktop shortcut location in admin installer")

    assert_contains(package_script, '$appIconPath = Join-Path $repoRoot "Assets\\Branding\\app-icon.ico"', "package icon path")
    assert_contains(package_script, "Test-Path $appIconPath -PathType Leaf", "package icon existence check")
    assert_contains(package_script, "$bundledVersionFileName = \"release-version.txt\"", "release-version.txt name")
    assert_contains(package_script, "Set-Content -Path $bundledVersionFilePath -Value $Version", "release-version.txt bundled exact version")

    assert_contains(icon_script, SOURCE_ICON_PATH, "icon generation source path")
    assert_contains(icon_script, ICON_PATH, "icon generation output path")
    assert_contains(icon_script, "ImageMagick was not found", "clear ImageMagick prerequisite error")
    assert_contains(icon_script, "Place the app icon source PNG there", "clear missing source image error")

    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL field")
    assert_contains(settings_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics disabled")

    assert_contains(backend_constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend constant")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend build lock")
    assert_contains(package_script, "$BackendBaseUrl -ne $productionBackendBaseUrl", "package backend lock")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend resolver returns production")
    assert_not_contains(inno.lower(), "localhost", "installer local backend behavior")

    print("Desktop app icon policy checks passed.")


if __name__ == "__main__":
    main()
