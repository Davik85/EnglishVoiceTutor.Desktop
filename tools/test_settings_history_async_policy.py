#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
UI_PATHS = [
    ROOT / "ViewModels" / "MainViewModel.cs",
    ROOT / "ViewModels" / "SettingsViewModel.cs",
    ROOT / "ViewModels" / "LessonHistoryViewModel.cs",
]
errors: list[str] = []

blocking_patterns = [
    (".GetAwaiter().GetResult()", re.compile(r"\.GetAwaiter\(\)\.GetResult\(\)")),
    (".Wait()", re.compile(r"\.Wait\s*\(")),
    (".Result", re.compile(r"\.Result\b")),
    ("Dispatcher.Invoke", re.compile(r"Dispatcher\.Invoke\b")),
]

for path in UI_PATHS:
    text = path.read_text(encoding="utf-8")
    for label, pattern in blocking_patterns:
        if pattern.search(text):
            errors.append(f"{path.relative_to(ROOT)} must not use blocking UI-thread pattern {label}.")

main_vm = (ROOT / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
settings_nav = re.search(r"private void NavigateToSettings\(Action navigateBack\).*?\n    private void SaveSettings", main_vm, re.S)
if not settings_nav:
    errors.append("Could not locate MainViewModel.NavigateToSettings.")
else:
    body = settings_nav.group(0)
    if "GetHistoryAsync" in body or "LoadVisibleCompletedLessonsForCurrentSessionAsync" in body:
        errors.append("Settings navigation must not require backend/local history completion before the view opens.")
    if "Array.Empty<LessonHistoryItem>()" not in body:
        errors.append("Settings navigation must pass an immediate safe fallback history snapshot.")

settings_vm = (ROOT / "ViewModels" / "SettingsViewModel.cs").read_text(encoding="utf-8")
constructor = re.search(r"public SettingsViewModel\(.*?\n    \[RelayCommand\]", settings_vm, re.S)
if not constructor:
    errors.append("Could not locate SettingsViewModel constructor.")
else:
    body = constructor.group(0)
    if "GetHistoryAsync" in body or "LoadVisibleCompletedLessonsForCurrentSessionAsync" in body:
        errors.append("SettingsViewModel constructor must not synchronously or directly load backend/history data.")
    if "ApplyLearningStatistics(lessonHistory);" not in body:
        errors.append("SettingsViewModel constructor should apply only the provided fallback history snapshot.")

if "await RefreshLearningStatisticsAsync();" not in settings_vm:
    errors.append("Settings async initialization/auth paths must refresh progress without blocking navigation.")

if "if (meResult.Status == AuthMeResultStatus.BackendUnavailable || meResult.User is null)" in settings_vm:
    backend_unavailable_block = re.search(
        r"if \(meResult\.Status == AuthMeResultStatus\.BackendUnavailable \|\| meResult\.User is null\).*?return;",
        settings_vm,
        re.S,
    )
    if backend_unavailable_block and "await RefreshLearningStatisticsAsync();" not in backend_unavailable_block.group(0):
        errors.append("Backend account-status failure must still allow async local progress refresh.")
else:
    errors.append("Could not find backend-unavailable restore-session branch.")

history_vm = (ROOT / "ViewModels" / "LessonHistoryViewModel.cs").read_text(encoding="utf-8")
if "_ = LoadHistoryAsync" not in history_vm:
    errors.append("Lesson History loading should be fire-and-forget from construction so navigation is not blocked.")
if "var localItems = await lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel);" not in history_vm:
    errors.append("Backend history failure must fall back to current-session local history asynchronously.")

history_service = (ROOT / "Services" / "LessonHistoryService.cs").read_text(encoding="utf-8")
if "if (string.IsNullOrWhiteSpace(ownerKey))" not in history_service or "return [];" not in history_service:
    errors.append("Current-session history must return empty when there is no authenticated owner.")
if "includeLegacyOwnerlessRecords: false" not in history_service:
    errors.append("Signed-in current-session history must hide ownerless legacy records.")
if "BuildOwnerKey(session?.User)" not in history_service:
    errors.append("Progress and history must derive their visible source from the current auth session owner.")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("Settings/history async policy checks passed: no UI blocking waits, Settings opens from fallback data, backend history is non-blocking, and account-scoped history is enforced.")
