from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
vm_path = ROOT / "ViewModels" / "LessonChatViewModel.cs"
vm = vm_path.read_text(encoding="utf-8")
errors: list[str] = []

required_constants = [
    "UiOperationWarningThresholdMilliseconds = 3000",
    "RealtimeStartupWarningThresholdMilliseconds = 8000",
    "TtsPlaybackPreparationWarningThresholdMilliseconds = 5000",
    "RecordingStopCommitWarningThresholdMilliseconds = 5000",
]
for constant in required_constants:
    if constant not in vm:
        errors.append(f"Missing diagnostics constant: {constant}")

for helper in ("StartUiOperationDiagnostics", "CompleteUiOperationDiagnostics", "UI operation start", "UI operation end", "UI operation warning"):
    if helper not in vm:
        errors.append(f"Missing diagnostics helper/log marker: {helper}")

required_operations = [
    "conversation_mode_enter",
    "conversation_mode_exit",
    "realtime_opening_playback",
    "realtime_recording_start",
    "realtime_recording_stop",
    "normal_recording_start",
    "normal_recording_stop",
    "play_voice",
    "auto_play_bot_voice",
    "finish_lesson",
    "lesson_back_navigation",
]
for operation in required_operations:
    if f'"{operation}"' not in vm:
        errors.append(f"Missing UI operation diagnostics for: {operation}")

for method in ("StartRealtimeVoiceRecordingAsync", "StopRealtimeVoiceRecordingAsync", "StartVoiceRecordingAsync", "StopVoiceRecordingAsync"):
    match = re.search(rf"private async Task {method}\(\).*?(?=\n    private |\n    \[RelayCommand|\Z)", vm, re.S)
    if not match:
        errors.append(f"Could not find method: {method}")
        continue
    body = match.group(0)
    if "finally" not in body:
        errors.append(f"{method} must use finally cleanup.")
    if "RefreshAllCommandStates();" not in body:
        errors.append(f"{method} must refresh command state in cleanup paths.")

ui_sensitive = vm
if re.search(r"\.(Wait|Result)\b", ui_sensitive):
    errors.append("UI-sensitive view model code must not use .Wait() or .Result.")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("Desktop hang diagnostics policy passed: named thresholds, start/end/duration logs, finally cleanup, and no blocking waits found.")
