#!/usr/bin/env python3
"""Deterministic checks that lesson openings are target-language aware."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require_text(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    localizer = read("Services/LocalizedLessonTextService.cs")
    backend = read("Services/LessonChatBackendService.cs")

    for needle in [
        "LocalizedLessonTextService.BuildSetupMessage",
        "Opening message created: Source=",
        "TargetLanguageId={this.studyLanguage.Id}",
        "InputScenarioMetadataOnly=True",
        "Starting lesson with StudyLanguageId=",
    ]:
        require_text(vm, needle, "ViewModels/LessonChatViewModel.cs")

    for needle in [
        "OpeningMessageSource",
        "Aujourd’hui, nous allons pratiquer",
        "The lesson JSON scenario text is semantic metadata.",
        "BuildContextOpeningLine",
        "BuildContextConfirmationLine",
        "BuildInvalidContextRedirect",
        "BuildFinalLessonMessage",
    ]:
        require_text(localizer, needle, "Services/LocalizedLessonTextService.cs")

    if "Today we'll practice" in localizer:
        raise AssertionError("Localized opening builder must not use the English opening as the non-English fallback.")

    require_text(backend, "InputLength={inputLength}; TargetLanguageId={resolvedTargetLanguage.Id}", "Services/LessonChatBackendService.cs")

    print("Multilingual opening message policy checks passed.")


if __name__ == "__main__":
    main()
