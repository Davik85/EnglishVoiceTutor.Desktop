#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
settings_vm = (ROOT / "ViewModels" / "SettingsViewModel.cs").read_text(encoding="utf-8")
history_vm = (ROOT / "ViewModels" / "LessonHistoryViewModel.cs").read_text(encoding="utf-8")
history_service = (ROOT / "Services" / "LessonHistoryService.cs").read_text(encoding="utf-8")
update_vm = settings_vm
update_download = (ROOT / "Services" / "Updates" / "UpdateDownloadService.cs").read_text(encoding="utf-8")
main_vm = (ROOT / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
errors: list[str] = []

restore = re.search(r"private async Task RestoreSessionAsync\(\).*?\n    \[RelayCommand\]", settings_vm, re.S)
if not restore:
    errors.append("RestoreSessionAsync must exist.")
else:
    body = restore.group(0)
    apply_pos = body.find("ApplyAuthenticatedUser(session.User)")
    refresh_pos = body.find("await RefreshLearningStatisticsAsync();", apply_pos)
    if apply_pos < 0 or refresh_pos < 0 or refresh_pos < apply_pos:
        errors.append("Session restore must apply the restored authenticated user before refreshing account-scoped progress/history.")
    backend_branch = re.search(r"if \(meResult\.Status == AuthMeResultStatus\.BackendUnavailable \|\| meResult\.User is null\).*?return;", body, re.S)
    if not backend_branch or "await RefreshLearningStatisticsAsync();" not in backend_branch.group(0):
        errors.append("Backend account/status failure must refresh local current-account history instead of clearing it.")

if "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync()" not in settings_vm:
    errors.append("Settings progress must use the current-session visible history source.")
if "lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel)" not in history_vm:
    errors.append("Lesson History must use the same current-session visible history source.")
if "LoadCompletedLessonsForOwnerKeys" not in history_service or "EmailOwnerPrefix + normalizedEmail" not in history_service:
    errors.append("Current-session history must match restored account records by user-id and email owner aliases.")
if "IsVisibleForAnyOwner" not in history_service:
    errors.append("Signed-in history must hide ownerless legacy records.")
if "ClearAsync" in update_vm or "AuthSessionStorageService" in update_vm or "LessonHistoryService" in update_download:
    errors.append("Update check/download code must not clear or mutate auth/session/history storage.")
logout = re.search(r"private async Task LogoutAsync\(\).*?\n    \[RelayCommand\]", settings_vm, re.S)
if not logout or "ClearAccountState();" not in logout.group(0) or "await RefreshLearningStatisticsAsync();" not in logout.group(0):
    errors.append("Logout must clear visible account state and refresh visible progress/history without deleting local records.")
if "ApplyAuthenticatedUser(result.Response.User)" not in settings_vm or "await RefreshLearningStatisticsAsync();" not in settings_vm:
    errors.append("Account switch/login must refresh progress/history for the new owner.")
if "Array.Empty<LessonHistoryItem>()" not in main_vm:
    errors.append("Settings may open with a temporary safe fallback while async auth/current owner refresh completes.")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)
print("Progress/history restore regression policy checks passed.")
