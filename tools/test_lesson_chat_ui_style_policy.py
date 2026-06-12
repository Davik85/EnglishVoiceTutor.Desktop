#!/usr/bin/env python3
"""Policy checks for Lesson Chat visual styling that protects release-critical settings."""
from __future__ import annotations

from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]

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


def read(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, label: str) -> None:
    require(needle in text, f"Missing {label}: {needle}")


def main() -> int:
    lesson_xaml = read("Views/LessonChatView.xaml")
    lesson_vm = read("ViewModels/LessonChatViewModel.cs")
    backend_constants = read("Constants/BackendConstants.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_code = read("Views/SettingsView.xaml.cs")

    for needle, label in [
        ("LineHeight = 22", "readable finish confirmation message line height"),
        ('TryFindResource("PrimaryButtonStyle")', "confirm action primary style"),
        ('TryFindResource("SecondaryButtonStyle")', "cancel action secondary style"),
    ]:
        require_text(lesson_vm, needle, label)
    require(lesson_vm.count("FontSize = 15") >= 3, "Finish confirmation message and both buttons must use aligned readable font size.")
    require(lesson_vm.count("MinHeight = 40") >= 2, "Both finish confirmation buttons must use balanced height.")
    require(lesson_vm.count("Padding = new Thickness(18, 8, 18, 8)") >= 2, "Both finish confirmation buttons must use consistent padding.")

    for needle, label in [
        ('x:Key="RecordingLessonButtonStyle"', "Start recording green style"),
        ('x:Key="LessonRecordingButtonBrush" Color="#FF2E7D4F"', "Start recording green normal brush"),
        ('x:Key="LessonRecordingButtonHoverBrush"', "Start recording hover brush"),
        ('x:Key="LessonRecordingButtonPressedBrush"', "Start recording pressed brush"),
        ('Style="{StaticResource RecordingLessonButtonStyle}"', "voice button uses green recording style"),
        ('x:Key="HintLessonButtonStyle"', "Hint warm style"),
        ('Background" Value="{StaticResource LessonSupportPanelBackgroundBrush}"', "Hint uses support panel background"),
        ('BorderBrush" Value="{StaticResource LessonSupportPanelBorderBrush}"', "Hint uses support panel border"),
        ('Style="{StaticResource HintLessonButtonStyle}"', "Hint button uses warm style"),
    ]:
        require_text(lesson_xaml, needle, label)

    require_text(backend_constants, 'ProductionBackendBaseUrl = "https://api.languagevoicetutor.com"', "release backend lock")
    require("DiagnosticsSection" not in settings_xaml, "Release Settings must not expose Diagnostics section.")
    require("Backend URL" not in settings_xaml and "BackendBaseUrl" not in settings_xaml, "Release Settings must not expose backend URL editing.")
    require_text(settings_code, "DesktopDiagnosticsEnabled = false", "release diagnostics UI disabled")

    tracked = subprocess.run(["git", "diff", "--cached", "--name-only"], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.splitlines()
    working = subprocess.run(["git", "diff", "--name-only"], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.splitlines()
    names = set(tracked + working)
    generated = [name for name in names if name.endswith(GENERATED_SUFFIXES) or any(marker in name for marker in GENERATED_DIR_MARKERS)]
    require(not generated, "Generated artifacts must not be committed or staged: " + ", ".join(sorted(generated)))

    print("Lesson Chat UI style policy checks passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: {exc}")
        raise SystemExit(1)
