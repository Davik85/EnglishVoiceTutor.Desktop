#!/usr/bin/env python3
"""Deterministic checks for feedback translation selection safety."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
LESSON_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def extract_method(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise AssertionError(f"Missing method: {signature}")
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


def main() -> int:
    vm = read(LESSON_VM)
    method = extract_method(vm, "private async Task ToggleFeedbackTranslationAsync()")

    assert_contains(method, "var feedback = SelectedFeedback;", "captured feedback before async boundary")
    assert_contains(method, "if (feedback is null)", "null selected feedback guard")
    assert_contains(method, "await TranslateSelectedFeedbackAsync(feedback);", "translation uses captured feedback")
    if "await TranslateSelectedFeedbackAsync(SelectedFeedback)" in method:
        raise AssertionError("Translation must not dereference SelectedFeedback after an async boundary.")
    if "StatusMessage = SelectedFeedback.ShortText" in method:
        raise AssertionError("Status must not dereference SelectedFeedback after translation await.")
    assert_contains(method, "if (!ReferenceEquals(SelectedFeedback, feedback))", "stale feedback selection guard")
    assert_contains(method, "SelectedFeedback?.ShortText ?? string.Empty", "safe stale loading status cleanup")
    assert_contains(method, "StatusMessage = feedback.ShortText;", "current selection status uses captured feedback")
    assert_contains(method, "catch (OperationCanceledException)", "translation cancellation guard")
    assert_contains(method, "catch", "translation failure guard")
    assert_contains(method, "finally", "translation cleanup finally")
    assert_contains(method, "isFeedbackTranslationLoading = false;", "translation loading reset")
    assert_contains(method, "RefreshAllCommandStates();", "command refresh after translation")
    assert_contains(vm, "private bool isFeedbackTranslationLoading;", "translation loading field")

    close_method = extract_method(vm, "private void CloseFeedback()")
    assert_contains(close_method, "SelectedFeedback = null;", "feedback close clears selection")
    assert_contains(close_method, "IsFeedbackTranslationVisible = false;", "feedback close hides translation")

    print("Feedback translation state policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
