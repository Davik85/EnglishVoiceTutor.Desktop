#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

history_service = (ROOT / "Services" / "LessonHistoryService.cs").read_text(encoding="utf-8")
main_vm = (ROOT / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
history_vm = (ROOT / "ViewModels" / "LessonHistoryViewModel.cs").read_text(encoding="utf-8")
settings_vm = (ROOT / "ViewModels" / "SettingsViewModel.cs").read_text(encoding="utf-8")
errors: list[str] = []

for needle in [
    "LoadCompletedLessons",
    "IsCompletedLessonRecord",
    "CountCompletedLessons",
    "GroupBy",
]:
    if needle not in history_service:
        errors.append(f"Lesson history source-of-truth policy missing: {needle}")

if "return LoadCompletedLessons();" not in history_service:
    errors.append("LessonHistoryService.Load must return sanitized completed-lesson records.")

if "Array.Empty<LessonHistoryItem>()" not in main_vm:
    errors.append("Settings navigation must be built with a safe fallback progress/history snapshot.")

if "LoadVisibleCompletedLessonsForCurrentSessionAsync().GetAwaiter().GetResult()" in main_vm:
    errors.append("Settings navigation must not synchronously load current-session lesson history.")

if "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel)" not in history_vm:
    errors.append("Lesson History view must use the same current-session visible completed-lesson source as Settings progress.")

if "RefreshLearningStatisticsAsync" not in settings_vm or "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync()" not in settings_vm:
    errors.append("Settings progress must refresh from the current-session visible lesson history source asynchronously.")

if "TotalCompletedLessonsText = lessonHistory.Count.ToString();" not in settings_vm:
    errors.append("Progress total must be derived from the completed lesson history collection count.")

if "includeLegacyOwnerlessRecords: false" not in history_service:
    errors.append("Signed-in current-session history must hide legacy ownerless local records.")

if "return [];" not in history_service:
    errors.append("Current-session history must safely return an empty collection without a signed-in owner.")

if "ClearAccountState();" not in settings_vm or "await RefreshLearningStatisticsAsync();" not in settings_vm:
    errors.append("Logout/session changes must clear account state and refresh visible progress/history.")

for forbidden in [
    "messages.Count",
    "turns.Count",
    "launch",
    "stale progress counter",
]:
    if forbidden in settings_vm.lower():
        errors.append(f"Progress appears to count a non-lesson source: {forbidden}")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("Progress/history consistency static policy checks passed.")
