#!/usr/bin/env python3
"""Policy checks for Windows installed executable/output naming."""
from __future__ import annotations

import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
NEW_BASE = "LanguageVoiceTutor.Desktop"
OLD_BASE = "EnglishVoiceTutor.Desktop"
OLD_FILES = [
    f"{OLD_BASE}.exe",
    f"{OLD_BASE}.dll",
    f"{OLD_BASE}.deps.json",
    f"{OLD_BASE}.runtimeconfig.json",
    f"{OLD_BASE}.pdb",
]


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


def git_tracked_artifacts() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "artifacts"],
        cwd=ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return [line for line in result.stdout.splitlines() if line.strip()]


def main() -> int:
    project = read("EnglishVoiceTutor.Desktop.csproj")
    inno = read("installer/windows/LanguageVoiceTutor.iss")
    package_inno = read("scripts/package-windows-inno-release.ps1")
    package_zip = read("scripts/package-tester-release.ps1")
    backend_constants = read("Constants/BackendConstants.cs")
    release_docs = "\n".join(
        read(path)
        for path in [
            "docs/WINDOWS_INSTALLER_RELEASE_FLOW.md",
            "docs/LOCAL_RELEASE.md",
            "docs/TESTER_RELEASE.md",
        ]
    )

    assert_contains(project, f"<AssemblyName>{NEW_BASE}</AssemblyName>", "desktop AssemblyName output")
    assert_not_contains(project, "<RootNamespace>LanguageVoiceTutor", "project-wide namespace rename")
    assert_contains(package_inno, f'$mainExe = "{NEW_BASE}.exe"', "Inno package executable check")
    assert_contains(package_zip, f'Join-Path $publishDirectory "{NEW_BASE}.exe"', "ZIP package executable check")

    assert_contains(inno, f'#define AppExeName "{NEW_BASE}.exe"', "Inno current executable define")
    assert_contains(inno, f'#define LegacyAppExeName "{OLD_BASE}.exe"', "Inno legacy executable define")
    assert_contains(inno, "AppId=LanguageVoiceTutor.Desktop", "stable Inno AppId")
    assert_contains(inno, "DefaultDirName={autopf}\\Language Voice Tutor", "default install directory")
    assert_contains(inno, "CloseApplications=yes", "close-running-app behavior")
    assert_contains(inno, "CloseApplicationsFilter={#AppExeName},{#LegacyAppExeName}", "new and legacy close process filter")
    assert_contains(inno, 'Filename: "{app}\\{#AppExeName}"', "shortcut and launch executable target")
    assert_not_contains(inno, 'Filename: "{app}\\EnglishVoiceTutor.Desktop.exe"', "hardcoded legacy shortcut or launch target")

    for old_file in OLD_FILES:
        assert_contains(inno, f'Type: files; Name: "{{app}}\\{old_file}"', f"install-folder cleanup for {old_file}")

    assert_not_contains(inno, "{userappdata}", "user AppData deletion")
    assert_not_contains(inno, "{localappdata}", "local AppData deletion")
    assert_not_contains(inno, "[UninstallDelete]", "uninstall/update app-data deletion section")
    assert_not_contains(inno, "auth-session.json", "auth session deletion")
    assert_not_contains(inno, "lesson-history.json", "lesson history deletion")

    assert_contains(backend_constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend constant")
    assert_contains(project, f"'$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend lock")
    assert_contains(package_inno, "Public direct-release installed builds are server-only", "packaging backend lock message")
    assert_contains(package_inno, "$BackendBaseUrl -ne $productionBackendBaseUrl", "packaging backend lock condition")

    assert_contains(release_docs, f"Installed tester/release output files now use `{NEW_BASE}.*` names", "installer naming decision docs")
    assert_contains(release_docs, "Internal project, folder, and namespace names may remain `EnglishVoiceTutor.*`", "safe internal-name docs")
    assert_not_contains(release_docs, "Existing executable name remains `EnglishVoiceTutor.Desktop.exe`", "obsolete executable-name decision docs")
    assert_not_contains(release_docs, "installed executable remains EnglishVoiceTutor.Desktop.exe", "obsolete installed executable docs")

    tracked_artifacts = git_tracked_artifacts()
    if tracked_artifacts:
        raise AssertionError("Generated artifacts are tracked by git: " + ", ".join(tracked_artifacts))

    print("Windows installer naming policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
