#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

history_item = (ROOT / "Models" / "LessonHistoryItem.cs").read_text(encoding="utf-8")
history_service = (ROOT / "Services" / "LessonHistoryService.cs").read_text(encoding="utf-8")
history_vm = (ROOT / "ViewModels" / "LessonHistoryViewModel.cs").read_text(encoding="utf-8")
settings_vm = (ROOT / "ViewModels" / "SettingsViewModel.cs").read_text(encoding="utf-8")
main_vm = (ROOT / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
backend_history_client = (ROOT / "Services" / "BackendLessonHistoryClient.cs").read_text(encoding="utf-8")

for needle in ["OwnerUserId", "OwnerEmail", "OwnerKey"]:
    if needle not in history_item:
        raise SystemExit(f"LessonHistoryItem must persist account owner metadata: {needle}")

for needle in [
    "BuildOwnerKey",
    "UserIdOwnerPrefix",
    "EmailOwnerPrefix",
    "LoadVisibleCompletedLessonsForCurrentSessionAsync",
    "includeLegacyOwnerlessRecords: false",
    "return [];",
    "IsVisibleForOwner",
    "GetItemOwnerKey",
    "AddForCurrentSessionAsync",
    "ApplyOwner",
]:
    if needle not in history_service:
        raise SystemExit(f"LessonHistoryService account scoping policy missing: {needle}")

if "LoadRawItems()" not in history_service or "LoadCompletedLessons().ToList()" in history_service:
    raise SystemExit("Saving a new account-scoped record must preserve raw legacy/other-account records instead of rewriting only the visible scope.")

if "LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel)" not in history_vm:
    raise SystemExit("Lesson History view must read only current-session visible account-scoped local history on backend fallback.")

if "backendResult.Succeeded" not in history_vm or "ReplaceItems(MapBackendItems" not in history_vm:
    raise SystemExit("Signed-in backend history success, including an empty response, must replace visible history instead of falling back to local data.")

if "LoadVisibleCompletedLessonsForCurrentSessionAsync()" not in settings_vm:
    raise SystemExit("Progress must use the same current-session visible account-scoped history source.")

for needle in ["RefreshLearningStatisticsAsync", "ClearAccountState()", "ApplyLearningStatistics"]:
    if needle not in settings_vm:
        raise SystemExit(f"Settings auth-state refresh policy missing: {needle}")

if "SaveLessonHistoryAsync" not in main_vm or "AddForCurrentSessionAsync" not in main_vm:
    raise SystemExit("New completed lesson records must be saved with the current authenticated owner when present.")

if "no authenticated session" not in backend_history_client or "session.AccessToken" not in backend_history_client:
    raise SystemExit("Backend history must only be used with an authenticated account session.")

print("Account-scoped lesson history/progress static policy checks passed.")
