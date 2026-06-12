#!/usr/bin/env python3
"""Policy checks for preserving local user data after the installed executable rename."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NEW_BASE = "LanguageVoiceTutor.Desktop"
OLD_BASE = "EnglishVoiceTutor.Desktop"


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


def main() -> int:
    storage_constants = read("Constants/StorageConstants.cs")
    migration = read("Services/LocalUserDataMigrationService.cs")
    auth_storage = read("Services/Auth/AuthSessionStorageService.cs")
    settings = read("Services/UserSettingsService.cs")
    history = read("Services/LessonHistoryService.cs")
    installer = read("installer/windows/LanguageVoiceTutor.iss")
    project = read("EnglishVoiceTutor.Desktop.csproj")
    backend_constants = read("Constants/BackendConstants.cs")
    docs = "\n".join(
        read(path)
        for path in [
            "docs/CURRENT_STATE.md",
            "docs/TESTER_RELEASE.md",
            "docs/WINDOWS_INSTALLER_RELEASE_FLOW.md",
            "docs/NEXT_STEPS.md",
        ]
    )

    assert_contains(project, f"<AssemblyName>{NEW_BASE}</AssemblyName>", "renamed output assembly")
    assert_contains(storage_constants, f'AppDataFolderName = "{NEW_BASE}"', "current product-owned app-data folder")
    assert_contains(storage_constants, f'"{OLD_BASE}"', "legacy EnglishVoiceTutor app-data folder")
    assert_contains(migration, "Environment.SpecialFolder.ApplicationData", "roaming app-data root")
    assert_contains(migration, "Environment.SpecialFolder.LocalApplicationData", "legacy local app-data root")
    assert_contains(migration, "CopyFirstLegacyFileToCurrentWhenMissing", "copy-only migration helper")
    assert_contains(migration, "if (File.Exists(currentFilePath))", "do not overwrite current files")
    assert_contains(migration, "File.Copy(legacyFilePath, currentFilePath, overwrite: false)", "safe copy semantics")
    assert_not_contains(migration, "AppContext.BaseDirectory", "user data must not depend on install directory")
    assert_not_contains(migration, "GetCurrentProcess", "user data must not depend on process name")

    assert_contains(auth_storage, 'ProtectedPayloadPurpose = "LanguageVoiceTutor.Desktop.AuthSession.v1"', "current DPAPI purpose")
    assert_contains(auth_storage, '"EnglishVoiceTutor.Desktop.AuthSession.v1"', "legacy DPAPI purpose")
    assert_contains(auth_storage, "AuthSessionFilePathCandidates", "auth session candidate paths")
    assert_contains(auth_storage, "await SaveAsync(migratedSession", "legacy auth session resaved to current path")
    assert_contains(auth_storage, "!StringComparer.OrdinalIgnoreCase.Equals(path, authSessionFilePath)", "current auth path skipped during migration")
    assert_contains(auth_storage, "ClearAsync", "logout clear method exists")
    assert_contains(auth_storage, "foreach (var sessionFilePath in authSessionFilePaths)", "logout clears current and legacy auth copies")

    for text, file_name, label in [
        (settings, "SettingsFileName", "settings"),
        (history, "LessonHistoryFileName", "lesson history/progress source"),
    ]:
        assert_contains(text, "LocalUserDataMigrationService.GetCurrentRoamingFilePath", f"{label} current path")
        assert_contains(text, f"StorageConstants.{file_name}", f"{label} file name constant")
        assert_contains(text, "CopyFirstLegacyFileToCurrentWhenMissing", f"{label} legacy migration")
        assert_contains(text, "includeLocalCurrentPath: false", f"{label} roaming-first path policy")

    assert_contains(installer, "[InstallDelete]", "install-folder cleanup section")
    assert_contains(installer, 'Name: "{app}\\EnglishVoiceTutor.Desktop.exe"', "legacy install-folder executable cleanup")
    assert_not_contains(installer, "{userappdata}", "installer must not delete roaming AppData")
    assert_not_contains(installer, "{localappdata}", "installer must not delete local AppData")
    assert_not_contains(installer, "auth-session.json", "installer must not delete auth session")
    assert_not_contains(installer, "lesson-history.json", "installer must not delete lesson history/progress source")
    assert_contains(backend_constants, 'ProductionBackendBaseUrl = "https://api.languagevoicetutor.com"', "release backend lock")
    assert_contains(project, "'$(DesktopBackendBaseUrl)' != 'https://api.languagevoicetutor.com'", "release backend validation")

    assert_contains(docs, f"Installed file names were renamed to `{NEW_BASE}.*`", "docs installed rename")
    assert_contains(docs, f"migrate preserved auth/session data from legacy `{OLD_BASE}` local-data paths", "docs auth migration")
    assert_contains(docs, "preserve login, settings, Lesson History, and Progress", "docs preservation goal")
    assert_contains(docs, "Do not state that the product is fully public production-ready", "docs avoid public production-ready overclaim")

    print("Desktop user-data rename migration policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
