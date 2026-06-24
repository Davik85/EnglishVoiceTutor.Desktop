#!/usr/bin/env python3
"""Deterministic checks for realtime tutor profile identity and guided roleplay policy."""
from __future__ import annotations

import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROMPT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonPromptBuilder.cs"
REALTIME_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "RealtimeVoiceSessionService.cs"
DESKTOP_VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"
LANA_PROFILE = ROOT / "Content" / "Tutors" / "lana.json"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def main() -> int:
    prompt_builder = read(PROMPT_BUILDER)
    realtime_service = read(REALTIME_SERVICE)
    tutor_guard = read(ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "TutorIdentityGuard.cs")
    desktop_vm = read(DESKTOP_VM)
    lana = json.loads(read(LANA_PROFILE))

    assert lana["displayName"] == "Lana"
    assert lana["homeCity"] == "London"
    assert lana["studies"] == "fashion design"
    assert "padel" in lana["hobbies"]
    assert "art" in lana["hobbies"]

    for needle in [
        "You are {avatarProfile.DisplayName}",
        "Lives in {avatarProfile.HomeCity}",
        "Studies {avatarProfile.Studies}",
        "FormatNaturalList(avatarProfile.Hobbies)",
    ]:
        assert_contains(prompt_builder, needle, "realtime Lana identity prompt construction")

    for needle in [
        "ResolveScenarioPlaceholders",
        "active tutor profile name",
        "Use 1-2 short sentences",
        "Use simple words",
        "Ask one simple question",
        "Avoid long explanations unless the learner asks",
    ]:
        assert_contains(prompt_builder, needle, "A1 realtime simplicity rule")

    for needle in [
        "This is active guided roleplay, not free conversation",
        "selected roleplay context",
        "Do not ask the learner to choose a new topic",
        "What would you like to " + '" + "discuss',
        "How can I " + '" + "assist',
    ]:
        assert_contains(prompt_builder, needle, "guided roleplay retention rule")

    for needle in [
        "TutorProfileId={TutorProfileId}",
        "TutorDisplayName={TutorDisplayName}",
        "SelectedContextTitle={SelectedContextTitle}",
    ]:
        assert_contains(realtime_service, needle, "backend realtime diagnostic")

    for needle in [
        "TutorProfileAge = tutorProfile.Age",
        "TutorProfileSpeakingRules = tutorProfile.SpeakingRules",
        "SelectedContextTitle={GetSelectedContextTitle()}",
        "RoleplayBeats = lessonScenario.RoleplayBeats",
        "GetSelectedContextOpeningLine()",
    ]:
        assert_contains(desktop_vm, needle, "desktop realtime tutor profile request")

    assert_contains(tutor_guard, "CommonWordsThatAreNotTutorNames", "tutor guard common-word false-positive filter")
    assert_contains(tutor_guard, "\"working\"", "tutor guard does not flag working as a tutor name")
    assert_contains(tutor_guard, "(?<name>[A-Z][a-z]+)", "tutor guard still detects capitalized wrong names such as Nastya")

    print("Tutor profile realtime policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
