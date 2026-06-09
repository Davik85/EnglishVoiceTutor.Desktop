#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

history_service = (ROOT / "Services" / "LessonHistoryService.cs").read_text(encoding="utf-8")
main_vm = (ROOT / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
history_vm = (ROOT / "ViewModels" / "LessonHistoryViewModel.cs").read_text(encoding="utf-8")
settings_vm = (ROOT / "ViewModels" / "SettingsViewModel.cs").read_text(encoding="utf-8")

for needle in [
    "LoadCompletedLessons",
    "IsCompletedLessonRecord",
    "CountCompletedLessons",
    "GroupBy",
]:
    if needle not in history_service:
        raise SystemExit(f"Lesson history source-of-truth policy missing: {needle}")

if "return LoadCompletedLessons();" not in history_service:
    raise SystemExit("LessonHistoryService.Load must return sanitized completed-lesson records.")

if "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync().GetAwaiter().GetResult();" not in main_vm:
    raise SystemExit("Settings progress must be initially built from the current-session visible lesson history source.")

if "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel)" not in history_vm:
    raise SystemExit("Lesson History view must use the same current-session visible completed-lesson source as Settings progress.")

if "TotalCompletedLessonsText = lessonHistory.Count.ToString();" not in settings_vm:
    raise SystemExit("Progress total must be derived from the completed lesson history collection count.")

for forbidden in [
    "messages.Count",
    "turns.Count",
    "launch",
    "stale progress counter",
]:
    if forbidden in settings_vm.lower():
        raise SystemExit(f"Progress appears to count a non-lesson source: {forbidden}")

print("Progress/history consistency static policy checks passed.")
