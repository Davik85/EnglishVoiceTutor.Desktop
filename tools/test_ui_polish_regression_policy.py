#!/usr/bin/env python3
"""Policy checks for Soft Learning Desktop UI polish regressions."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
DESIGN_SYSTEM = ROOT / "Resources" / "DesignSystem.xaml"
LEVEL_VIEW = ROOT / "Views" / "LevelSelectionView.xaml"
HOME_VIEW = ROOT / "Views" / "HomeView.xaml"
LESSON_CHAT_VIEW = ROOT / "Views" / "LessonChatView.xaml"
REALTIME_SCHEMA_TESTS = [
    ROOT / "tools" / "test_realtime_ga_session_schema.py",
    ROOT / "tools" / "test_realtime_ga_content_schema.py",
]


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def extract_first_tag(text: str, tag: str, content_binding: str) -> str:
    match = re.search(rf"<{tag}[^>]*Content=\"{{Binding {re.escape(content_binding)}}}\"[^>]*/?>", text)
    if not match:
        raise AssertionError(f"Missing {tag} bound to {content_binding}.")
    return match.group(0)


def assert_no_small_fixed_width(tag_text: str, label: str, minimum: int) -> None:
    width_match = re.search(r"\bWidth=\"(\d+)\"", tag_text)
    if width_match and int(width_match.group(1)) < minimum:
        raise AssertionError(f"{label} uses fixed Width={width_match.group(1)}, below {minimum}.")


def assert_min_width(tag_text: str, label: str, minimum: int) -> None:
    match = re.search(r"\bMinWidth=\"(\d+)\"", tag_text)
    if not match or int(match.group(1)) < minimum:
        raise AssertionError(f"{label} must have MinWidth >= {minimum}.")


def main() -> int:
    design = read(DESIGN_SYSTEM)
    level = read(LEVEL_VIEW)
    home = read(HOME_VIEW)
    chat = read(LESSON_CHAT_VIEW)

    assert_contains(design, "HorizontalContentAlignment", "shared button horizontal content centering")
    assert_contains(design, "VerticalContentAlignment", "shared button vertical content centering")
    assert_contains(design, "SelectableChatTextBoxStyle", "selectable chat text style")
    for selectable_property in [
        "IsReadOnly\" Value=\"True",
        "TextWrapping\" Value=\"Wrap",
        "Background\" Value=\"Transparent",
        "BorderThickness\" Value=\"0",
    ]:
        assert_contains(design, selectable_property, f"selectable chat text {selectable_property}")

    assert_contains(level, "<ScrollViewer", "level selection scroll safety")
    assert_contains(level, "VerticalAlignment=\"Top\"", "level card top alignment inside scroll viewer")
    assert_contains(level, "Content=\"{Binding ContinueButtonText}\"", "level continue button")
    assert_contains(level, "Content=\"{Binding BackButtonText}\"", "level back button")
    for binding, minimum in [("ContinueButtonText", 140), ("BackButtonText", 140)]:
        tag = extract_first_tag(level, "Button", binding)
        assert_min_width(tag, f"level {binding}", minimum)
        if re.search(r"\bHeight=\"", tag):
            raise AssertionError(f"level {binding} must not use fixed Height that can crop content.")

    history_button = extract_first_tag(home, "Button", "HistoryButtonText")
    assert_min_width(history_button, "home lesson history button", 150)
    assert_no_small_fixed_width(history_button, "home lesson history button", 150)
    assert_contains(home, "Content=\"{Binding SettingsButtonText}\"", "home settings button")
    settings_button = extract_first_tag(home, "Button", "SettingsButtonText")
    assert_min_width(settings_button, "home settings button", 120)
    assert_contains(home, "Margin=\"6,10,6,0\"", "settings second-row spacing")

    finish_button = extract_first_tag(chat, "Button", "FinishLessonButtonText")
    assert_min_width(finish_button, "lesson finish button", 150)
    assert_no_small_fixed_width(finish_button, "lesson finish button", 150)
    for binding, minimum in [
        ("SendButtonText", 96),
        ("VoiceButtonText", 170),
        ("HintButtonText", 96),
        ("BackButtonText", 96),
    ]:
        tag = extract_first_tag(chat, "Button", binding)
        assert_min_width(tag, f"lesson {binding}", minimum)

    assert_contains(chat, "AcceptsReturn=\"False\"", "single-line input documents Shift+Enter no-op behavior")
    if "Key=\"Enter\"" in chat:
        raise AssertionError("LessonChatView.xaml must not use Key=\"Enter\" because WPF XAML parsing requires Key=\"Return\" for the main Enter key.")

    has_return_key_binding = "<KeyBinding Key=\"Return\"" in chat
    has_safe_preview_handler = "PreviewKeyDown=\"" in chat and "SendMessageCommand" in read(ROOT / "Views" / "LessonChatView.xaml.cs")
    if not (has_return_key_binding or has_safe_preview_handler):
        raise AssertionError("Lesson text input must support Enter-to-send via Key=\"Return\" or a named safe PreviewKeyDown handler.")

    if has_return_key_binding:
        assert_contains(chat, "<TextBox.InputBindings>", "lesson text input bindings")
        assert_contains(chat, "Modifiers=\"None\"", "Enter sends only without Shift modifiers")
        assert_contains(chat, "Command=\"{Binding SendMessageCommand}\"", "Enter invokes existing send command")

    if chat.count("Style=\"{StaticResource SelectableChatTextBoxStyle}\"") < 2:
        raise AssertionError("Chat body and translation text must use selectable read-only TextBox styling.")
    for command_binding in ["ToggleTranslationCommand", "PlayBotVoiceCommand", "ViewFeedbackCommand"]:
        assert_contains(chat, command_binding, f"chat action remains available: {command_binding}")

    for realtime_test in REALTIME_SCHEMA_TESTS:
        assert_contains(read(realtime_test), "realtime", f"existing realtime policy test still present: {realtime_test.name}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
