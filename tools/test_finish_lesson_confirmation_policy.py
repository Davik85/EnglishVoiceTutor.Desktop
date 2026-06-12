#!/usr/bin/env python3
"""Regression checks for accidental early Finish lesson protection."""
from __future__ import annotations

from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
LESSON_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"
APP_TEXT = ROOT / "Localization" / "AppLocalizedText.cs"
APP_LOCALIZATION = ROOT / "Localization" / "AppLocalization.cs"
BACKEND_LOCK_POLICY = ROOT / "tools" / "test_desktop_release_backend_lock_policy.py"
PROGRESS_HISTORY_POLICY = ROOT / "tools" / "test_progress_history_restore_regression_policy.py"

GENERATED_SUFFIXES = (
    ".exe",
    ".msi",
    ".zip",
    ".bak",
    ".tmp",
    ".log",
    ".sql",
    ".mp4",
    ".mov",
    ".png",
    ".jpg",
    ".jpeg",
)
GENERATED_DIR_MARKERS = ("AppData/", "artifacts/", "release/", "installer/", "publish/")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, label: str) -> None:
    require(needle in text, f"Missing {label}: {needle}")


def extract_method(text: str, signature: str) -> str:
    start = text.find(signature)
    require(start >= 0, f"Missing method: {signature}")
    brace = text.find("{", start)
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise AssertionError(f"Could not extract method: {signature}")


def ensure_policy_passes(script: Path, label: str) -> None:
    subprocess.run(["python", str(script)], cwd=ROOT, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)


def main() -> int:
    lesson_vm = read(LESSON_VM)
    app_text = read(APP_TEXT)
    app_localization = read(APP_LOCALIZATION)

    finish_method = extract_method(lesson_vm, "private async Task FinishLesson()")
    require_text(finish_method, "ShouldConfirmManualEarlyFinish() && !ShowFinishLessonConfirmation()", "early manual finish confirmation gate")
    require_text(finish_method, "CompleteLesson();", "existing finish flow remains called")
    require_text(finish_method, "isFinishLessonInProgress = true;", "finish in-progress guard starts")
    require_text(finish_method, "isFinishLessonInProgress = false;", "finish in-progress guard clears")
    require_text(finish_method, "if (isFinishLessonInProgress || isFinishLessonConfirmationOpen)", "duplicate finish execution guard")

    confirm_predicate = extract_method(lesson_vm, "private bool ShouldConfirmManualEarlyFinish()")
    require_text(confirm_predicate, "!IsCompletedAwaitingFinish", "forced/completed-awaiting-finish skips confirmation")
    require_text(confirm_predicate, "!IsLessonLimitReached", "final lesson limit skips confirmation")
    require_text(confirm_predicate, "CurrentLessonPhase == LessonPhase.SetupContextSelection || CurrentLessonPhase == LessonPhase.ActiveRoleplay", "only active lesson phases require confirmation")

    dialog_method = extract_method(lesson_vm, "private bool ShowFinishLessonConfirmation()")
    for needle, label in [
        ("isFinishLessonConfirmationOpen = true;", "confirmation-open guard starts"),
        ("isFinishLessonConfirmationOpen = false;", "confirmation-open guard clears"),
        ("return dialog.ShowDialog() == true;", "cancel/close does not finish"),
        ("localizedText.FinishLessonConfirmationTitle", "localized title"),
        ("localizedText.FinishLessonConfirmationMessage", "localized message"),
        ("localizedText.FinishLessonConfirmationConfirmButtonText", "localized confirm button"),
        ("localizedText.FinishLessonConfirmationCancelButtonText", "localized cancel button"),
    ]:
        require_text(dialog_method, needle, label)

    complete_method = extract_method(lesson_vm, "private void CompleteLesson()")
    for needle, label in [
        ("_ = TryFinishBackendLessonSessionAsync(\"finish_lesson\");", "backend finish/history flow"),
        ("finishLesson(BuildLessonSummaryInput(), backendLessonSessionId);", "summary navigation flow"),
        ("LogDeveloperLessonUsageSummary(\"finish_lesson\");", "usage summary logging"),
    ]:
        require_text(complete_method, needle, label)

    can_finish = extract_method(lesson_vm, "private bool CanFinishLesson()")
    require_text(can_finish, "!isFinishLessonInProgress", "command disabled while finish runs")
    require_text(can_finish, "!isFinishLessonConfirmationOpen", "command disabled while confirmation is open")

    for needle in [
        "FinishLessonConfirmationTitle",
        "FinishLessonConfirmationMessage",
        "FinishLessonConfirmationConfirmButtonText",
        "FinishLessonConfirmationCancelButtonText",
    ]:
        require_text(app_text, needle, f"localized text property {needle}")
    for needle in [
        'l("Finish lesson?")',
        'l("This lesson is still active. Do you want to finish it now?")',
        'l("Continue lesson")',
    ]:
        require_text(app_localization, needle, f"localized copy {needle}")

    back_method = extract_method(lesson_vm, "private async Task Back()")
    require("ShowFinishLessonConfirmation" not in back_method and "ShouldConfirmManualEarlyFinish" not in back_method, "Back navigation must remain unchanged by finish confirmation.")

    ensure_policy_passes(PROGRESS_HISTORY_POLICY, "progress/history policy")
    ensure_policy_passes(BACKEND_LOCK_POLICY, "release backend lock policy")
    tracked = subprocess.run(["git", "diff", "--cached", "--name-only"], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.splitlines()
    working = subprocess.run(["git", "diff", "--name-only"], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.splitlines()
    names = set(tracked + working)
    generated = [name for name in names if name.endswith(GENERATED_SUFFIXES) or any(marker in name for marker in GENERATED_DIR_MARKERS)]
    require(not generated, "Generated artifacts must not be committed or staged: " + ", ".join(sorted(generated)))

    print("Finish lesson confirmation policy checks passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: {exc}")
        raise SystemExit(1)
