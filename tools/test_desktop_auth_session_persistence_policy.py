#!/usr/bin/env python3
"""Static policy checks for desktop authenticated session persistence."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]

FILES = {
    "storage": ROOT / "Services/Auth/AuthSessionStorageService.cs",
    "auth_backend": ROOT / "Services/Auth/AuthBackendService.cs",
    "stored_session": ROOT / "Models/Auth/StoredAuthSession.cs",
    "auth_response": ROOT / "Models/Auth/AuthResponse.cs",
    "main_vm": ROOT / "ViewModels/MainViewModel.cs",
    "settings_vm": ROOT / "ViewModels/SettingsViewModel.cs",
    "history_service": ROOT / "Services/LessonHistoryService.cs",
    "installer": ROOT / "installer/windows/LanguageVoiceTutor.iss",
    "current_state": ROOT / "docs/CURRENT_STATE.md",
    "next_steps": ROOT / "docs/NEXT_STEPS.md",
    "tester_release": ROOT / "docs/TESTER_RELEASE.md",
    "windows_upload": ROOT / "docs/WINDOWS_RELEASE_SERVER_UPLOAD.md",
}


def read(name: str) -> str:
    path = FILES[name]
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
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


def main() -> int:
    storage = read("storage")
    auth_backend = read("auth_backend")
    stored_session = read("stored_session")
    auth_response = read("auth_response")
    main_vm = read("main_vm")
    settings_vm = read("settings_vm")
    history_service = read("history_service")
    installer = read("installer")
    docs = "\n".join(read(name) for name in ["current_state", "next_steps", "tester_release", "windows_upload"])

    # Secure local storage shape: app-data file, DPAPI current-user protection, no raw password field.
    assert_contains(storage, "Environment.SpecialFolder.ApplicationData", "auth session app-data location")
    assert_contains(storage, "StorageConstants.AuthSessionFileName", "auth session file constant")
    assert_contains(storage, "ProtectedData.Protect", "DPAPI protect call")
    assert_contains(storage, "ProtectedData.Unprotect", "DPAPI unprotect call")
    assert_contains(storage, "DataProtectionScope.CurrentUser", "current-user DPAPI scope")
    assert_contains(storage, "ProtectedPayloadPurpose", "purpose-bound DPAPI entropy")
    assert_contains(storage, "Convert.ToBase64String", "protected payload written as encoded blob")
    assert_not_contains(stored_session.lower(), "password", "password field in stored session model")
    assert_not_contains(auth_response.lower(), "password", "password field in auth response model")

    # Missing, corrupt, plaintext legacy, and expired storage must not crash or keep using bad credentials.
    assert_contains(storage, "if (!File.Exists(authSessionFilePath))", "missing auth-session tolerance")
    assert_contains(storage, "await ClearAsync(cancellationToken);", "invalid auth-session cleanup")
    assert_contains(storage, "catch", "corrupt auth-session guarded load")
    assert_contains(storage, "MigratedFromPlainText", "legacy plaintext migration path")
    assert_contains(storage, "IsExpired", "stored token expiry check")
    assert_regex(storage, r"IsExpired\(StoredAuthSession session\).*ExpiresAtUtc\s*<=\s*DateTimeOffset\.UtcNow", "expiry rejects expired tokens")

    # Login/register persist, logout clears, /me validation clears only rejected sessions.
    assert_contains(auth_backend, "return AuthenticateAsync(BackendConstants.AuthRegisterEndpoint", "register goes through persistence path")
    assert_contains(auth_backend, "return AuthenticateAsync(BackendConstants.AuthLoginEndpoint", "login goes through persistence path")
    assert_contains(auth_backend, "await sessionStorageService.SaveAsync(storedSession", "successful auth persists stored session")
    assert_contains(auth_backend, "payload.AccessToken", "access token persisted from auth response")
    assert_contains(auth_backend, "payload.ExpiresAtUtc", "expiry persisted from auth response")
    assert_contains(auth_backend, "payload.User", "user identity persisted from auth response")
    assert_contains(auth_backend, "sessionStorageService.GetValidSessionOrNullAsync", "restore uses validity check")
    assert_contains(auth_backend, "return sessionStorageService.ClearAsync", "logout clears persisted session")
    assert_contains(auth_backend, "response.StatusCode == HttpStatusCode.Unauthorized", "backend rejection detection")
    assert_contains(auth_backend, "return AuthMeResult.BackendUnavailable();", "backend outage does not invalidate session")

    # Startup and settings restore account before account-scoped history/progress loads, with no blocking async pattern.
    assert_contains(main_vm, "_ = TryRestoreSavedAuthSessionOnStartupAsync();", "non-blocking startup restore")
    assert_contains(main_vm, "await authBackendService.TryRestoreSessionAsync();", "startup restore reads persisted session")
    assert_contains(main_vm, "StoredSessionCleared=False", "backend-unavailable restore keeps stored session")
    assert_contains(settings_vm, "var session = await authBackendService.TryRestoreSessionAsync();", "settings restore reads persisted session")
    assert_regex(settings_vm, r"ApplyAuthenticatedUser\(session\.User\);.*await RefreshLearningStatisticsAsync\(\);", "cached user applied before scoped statistics refresh")
    assert_regex(settings_vm, r"await authBackendService\.LogoutAsync\(\);\s*ClearAccountState\(\);\s*await LoadSettingsForCurrentSessionAsync\(\);\s*await RefreshLearningStatisticsAsync\(\);", "logout clears visible account state and statistics")
    assert_contains(main_vm, "Array.Empty<LessonHistoryItem>()", "settings starts without unscoped history flash")
    assert_contains(history_service, "LoadVisibleCompletedLessonsForCurrentSessionAsync", "history is scoped to current restored session")
    assert_contains(history_service, "includeLegacyOwnerlessRecords: false", "signed-in visible history excludes ownerless records")
    for text, label in [(main_vm, "main view model"), (settings_vm, "settings view model"), (auth_backend, "auth backend"), (storage, "auth storage")]:
        assert_not_contains(text, ".Result", f"blocking async .Result in {label}")
        assert_not_contains(text, ".Wait(", f"blocking async .Wait in {label}")
        assert_not_contains(text, "GetAwaiter().GetResult()", f"blocking async GetAwaiter/GetResult in {label}")

    # Installer/update path must preserve app-data auth storage.
    assert_not_contains(installer, "{userappdata}", "installer must not delete roaming app-data session")
    assert_contains(docs, "Reinstall/update should preserve user app data and session storage", "session preservation docs")
    assert_contains(docs, "Logout clears persisted auth session", "logout docs")
    assert_contains(docs, "does not store raw passwords", "raw password docs")
    assert_contains(docs, "Desktop authenticated session persistence is now part of the tester-readiness foundation", "tester-readiness docs")
    assert_contains(docs, "Same-version installer reinstall confirmation remains in place", "same-version reinstall docs")
    assert_contains(docs, "This is still not the future in-app update UI", "future update UI separation docs")
    assert_contains(docs, "latest.json", "future latest.json docs")
    assert_contains(docs, "SHA-256", "future SHA-256 docs")
    assert_contains(docs, "active-lesson-safe update flow", "active lesson safe update docs")
    assert_contains(docs, "External tester handoff remains blocked", "external handoff blocker docs")

    print("Desktop auth session persistence policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
