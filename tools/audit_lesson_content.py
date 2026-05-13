#!/usr/bin/env python3
"""Audit English Voice Tutor lesson JSON and routing consistency."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
LESSONS_ROOT = REPO_ROOT / "Content" / "Lessons"

LEVELS = [
    "A1 Beginner",
    "A2 Elementary",
    "B1 Intermediate",
    "B2 Upper-Intermediate",
]

EXPECTED_REGISTRY = {
    "EverydayEnglish": {
        "topic": "Everyday English",
        "files": {
            "introductions.json": "Introductions",
            "small_talk_with_a_neighbor.json": "Small talk with a neighbor",
            "asking_for_help.json": "Asking for help",
            "making_plans.json": "Making plans",
            "talking_about_your_day.json": "Talking about your day",
        },
    },
    "Travel": {
        "topic": "Travel",
        "files": {
            "airport_check_in.json": "Airport check-in",
            "hotel_check_in.json": "Hotel check-in",
            "asking_for_directions.json": "Asking for directions",
            "ordering_transport.json": "Ordering transport",
            "lost_luggage.json": "Lost luggage",
        },
    },
    "WorkAndBusiness": {
        "topic": "Work & Business",
        "files": {
            "first_meeting.json": "First meeting",
            "daily_standup.json": "Daily standup",
            "phone_call_with_a_client.json": "Phone call with a client",
            "asking_for_clarification.json": "Asking for clarification",
            "discussing_deadlines.json": "Discussing deadlines",
        },
    },
    "JobInterview": {
        "topic": "Job Interview",
        "files": {
            "tell_me_about_yourself.json": "Tell me about yourself",
            "work_experience.json": "Work experience",
            "strengths_and_weaknesses.json": "Strengths and weaknesses",
            "why_do_you_want_this_job.json": "Why do you want this job?",
            "asking_questions_at_the_end.json": "Asking questions at the end",
        },
    },
    "RestaurantAndCafe": {
        "topic": "Restaurant & Cafe",
        "files": {
            "booking_a_table.json": "Booking a table",
            "ordering_food.json": "Ordering food",
            "asking_about_ingredients.json": "Asking about ingredients",
            "handling_a_wrong_order.json": "Handling a wrong order",
            "paying_the_bill.json": "Paying the bill",
        },
    },
    "FreeConversation": {
        "topic": "Free Conversation",
        "files": {
            "open_conversation.json": "Open conversation",
        },
    },
}

TOP_LEVEL_REQUIRED = [
    "id",
    "metadata",
    "lessonSetup",
    "learningGoal",
    "situation",
    "roles",
    "targetLanguage",
    "levelProfiles",
    "conversationFlow",
    "controlledVariation",
    "offTopicHandling",
    "feedbackRules",
    "hintRules",
    "repetitionLogic",
    "aiTutorPromptInstructions",
]

METADATA_REQUIRED = [
    "topic",
    "subtopic",
    "lessonType",
    "supportedLevels",
    "softWrapUpAfterUserTurn",
    "finalMessageAtUserTurn",
    "setupAndContextChoiceCountAsLessonTurns",
]

LEVEL_PROFILE_REQUIRED = [
    "level",
    "difficultyNotes",
    "tutorLanguageStyle",
    "expectedUserResponse",
    "minimumUserResponse",
    "stretchUserResponse",
    "addedKeyPhrases",
    "addedUsefulConstructions",
    "addedGrammarFocus",
    "feedbackStrictness",
    "hintStrategy",
    "correctionPriority",
    "conversationDepth",
    "exampleGoodAnswer",
    "exampleStretchAnswer",
    "softWrapUpAfterUserTurn",
    "finalMessageAtUserTurn",
]

ALLOWED_LESSON_TYPES = {"guided_roleplay", "free_conversation"}
CYRILLIC_PATTERN = re.compile(r"[А-Яа-яЁё]")
GENERIC_PHRASES = [
    "Use one short",
    "clear subject and verb",
    "simple word order",
    "Make the request, then add one specific detail",
    "Answer the staff question directly",
    "Let us",
]
FAIL_GENERIC_PHRASES = {"Let us"}
OBSOLETE_LEVEL_FOLDERS = ["A1", "A2", "B1", "B2"]


def rel(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


class AuditReport:
    def __init__(self) -> None:
        self.errors: list[str] = []
        self.warnings: list[str] = []
        self.infos: list[str] = []

    def error(self, message: str) -> None:
        self.errors.append(message)

    def warn(self, message: str) -> None:
        self.warnings.append(message)

    def info(self, message: str) -> None:
        self.infos.append(message)


def load_json_files(report: AuditReport) -> dict[Path, Any]:
    parsed: dict[Path, Any] = {}
    if not LESSONS_ROOT.exists():
        report.error(f"Missing lessons root: {rel(LESSONS_ROOT)}")
        return parsed

    for path in sorted(LESSONS_ROOT.rglob("*.json")):
        try:
            parsed[path] = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            report.error(f"Invalid JSON in {rel(path)}: line {exc.lineno}, column {exc.colno}: {exc.msg}")
    return parsed


def check_folders_and_registry(parsed: dict[Path, Any], report: AuditReport) -> None:
    for folder_name, registry in EXPECTED_REGISTRY.items():
        folder = LESSONS_ROOT / folder_name
        if not folder.is_dir():
            report.error(f"Expected topic folder is missing: {rel(folder)}")
            continue

        expected_files = set(registry["files"].keys())
        actual_files = {path.name for path in folder.glob("*.json")}

        for missing in sorted(expected_files - actual_files):
            report.error(f"Expected lesson file is missing: {rel(folder / missing)}")

        for extra in sorted(actual_files - expected_files):
            report.error(f"Unexpected lesson JSON under known topic folder: {rel(folder / extra)}")

    known_folders = set(EXPECTED_REGISTRY)
    for path in sorted(LESSONS_ROOT.iterdir() if LESSONS_ROOT.exists() else []):
        if path.is_dir() and path.name not in known_folders and path.name not in OBSOLETE_LEVEL_FOLDERS:
            report.warn(f"Unregistered topic folder found under Content/Lessons: {rel(path)}")

    for path, data in parsed.items():
        folder = path.parent.name
        registry = EXPECTED_REGISTRY.get(folder)
        if not registry or path.name not in registry["files"]:
            continue
        metadata = data.get("metadata", {}) if isinstance(data, dict) else {}
        expected_topic = registry["topic"]
        expected_subtopic = registry["files"][path.name]
        if metadata.get("topic") != expected_topic:
            report.error(f"{rel(path)} metadata.topic is {metadata.get('topic')!r}; expected {expected_topic!r}")
        if metadata.get("subtopic") != expected_subtopic:
            report.error(f"{rel(path)} metadata.subtopic is {metadata.get('subtopic')!r}; expected {expected_subtopic!r}")


def check_obsolete_folders(report: AuditReport) -> None:
    for folder_name in OBSOLETE_LEVEL_FOLDERS:
        folder = LESSONS_ROOT / folder_name
        if folder.exists():
            report.error(f"Obsolete per-level lesson folder exists: {rel(folder)}")


def check_text_content(report: AuditReport) -> None:
    for path in sorted(LESSONS_ROOT.rglob("*.json")):
        text = path.read_text(encoding="utf-8")
        for match in CYRILLIC_PATTERN.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            report.error(f"Cyrillic content found in {rel(path)}:{line}: {match.group(0)!r}")

        is_free_conversation = "Content/Lessons/FreeConversation/" in rel(path) + "/"
        for phrase in GENERIC_PHRASES:
            if phrase in text:
                message = f"Generic/copied phrase found in {rel(path)}: {phrase!r}"
                if phrase in FAIL_GENERIC_PHRASES or is_free_conversation:
                    report.error(message)
                else:
                    report.warn(message)


def check_required_fields(parsed: dict[Path, Any], report: AuditReport) -> None:
    for path, data in parsed.items():
        if not isinstance(data, dict):
            report.error(f"{rel(path)} top-level JSON value must be an object")
            continue

        for field in TOP_LEVEL_REQUIRED:
            if field not in data:
                report.error(f"{rel(path)} missing top-level field: {field}")

        metadata = data.get("metadata")
        if not isinstance(metadata, dict):
            report.error(f"{rel(path)} metadata must be an object")
            continue

        for field in METADATA_REQUIRED:
            if field not in metadata:
                report.error(f"{rel(path)} metadata missing field: {field}")

        lesson_type = metadata.get("lessonType")
        if lesson_type not in ALLOWED_LESSON_TYPES:
            report.error(f"{rel(path)} metadata.lessonType is {lesson_type!r}; expected one of {sorted(ALLOWED_LESSON_TYPES)}")

        supported = metadata.get("supportedLevels")
        if supported != LEVELS:
            report.error(f"{rel(path)} metadata.supportedLevels must exactly match {LEVELS!r}")

        check_level_profiles(path, data, lesson_type, report)
        check_lesson_type_specific_content(path, data, lesson_type, report)


def check_level_profiles(path: Path, data: dict[str, Any], lesson_type: str, report: AuditReport) -> None:
    profiles = data.get("levelProfiles")
    if not isinstance(profiles, dict):
        report.error(f"{rel(path)} levelProfiles must be an object")
        return

    for level in LEVELS:
        profile = profiles.get(level)
        if not isinstance(profile, dict):
            report.error(f"{rel(path)} missing levelProfiles entry: {level}")
            continue

        for field in LEVEL_PROFILE_REQUIRED:
            if field not in profile:
                report.error(f"{rel(path)} levelProfiles.{level} missing field: {field}")

        if profile.get("level") != level:
            report.error(f"{rel(path)} levelProfiles.{level}.level is {profile.get('level')!r}; expected {level!r}")

        expected_soft, expected_final = expected_limits(lesson_type, level)
        if profile.get("softWrapUpAfterUserTurn") != expected_soft:
            report.error(
                f"{rel(path)} levelProfiles.{level}.softWrapUpAfterUserTurn is "
                f"{profile.get('softWrapUpAfterUserTurn')!r}; expected {expected_soft}"
            )
        if profile.get("finalMessageAtUserTurn") != expected_final:
            report.error(
                f"{rel(path)} levelProfiles.{level}.finalMessageAtUserTurn is "
                f"{profile.get('finalMessageAtUserTurn')!r}; expected {expected_final}"
            )


def expected_limits(lesson_type: str, level: str) -> tuple[int, int]:
    if lesson_type == "free_conversation":
        return 25, 30
    if level in {"A1 Beginner", "A2 Elementary"}:
        return 10, 15
    return 20, 25


def check_lesson_type_specific_content(path: Path, data: dict[str, Any], lesson_type: str, report: AuditReport) -> None:
    controlled = data.get("controlledVariation", {})
    variants = controlled.get("contextVariants", []) if isinstance(controlled, dict) else []
    instructions = data.get("aiTutorPromptInstructions", [])
    instructions_text = "\n".join(str(item) for item in instructions) if isinstance(instructions, list) else ""

    if lesson_type == "guided_roleplay":
        if not isinstance(variants, list) or not variants:
            report.error(f"{rel(path)} guided_roleplay lesson must define controlledVariation.contextVariants")
    elif lesson_type == "free_conversation":
        required_safety_terms = [
            "safe",
            "harmful",
            "illegal",
            "self-harm",
            "hateful",
            "sexually explicit",
            "professional medical",
            "redirect",
        ]
        missing = [term for term in required_safety_terms if term.lower() not in instructions_text.lower()]
        if missing:
            report.error(f"{rel(path)} free_conversation aiTutorPromptInstructions missing safety terms: {', '.join(missing)}")


def check_routing_sources(report: AuditReport) -> None:
    topic_folder_constants = [
        "ContentConstants.EverydayEnglishFolderName",
        "ContentConstants.TravelFolderName",
        "ContentConstants.WorkAndBusinessFolderName",
        "ContentConstants.JobInterviewFolderName",
        "ContentConstants.RestaurantAndCafeFolderName",
        "ContentConstants.FreeConversationFolderName",
    ]
    lesson_file_constants = [
        "ContentConstants.IntroductionsFileName",
        "ContentConstants.SmallTalkWithANeighborFileName",
        "ContentConstants.AskingForHelpFileName",
        "ContentConstants.MakingPlansFileName",
        "ContentConstants.TalkingAboutYourDayFileName",
        "ContentConstants.AirportCheckInFileName",
        "ContentConstants.HotelCheckInFileName",
        "ContentConstants.AskingForDirectionsFileName",
        "ContentConstants.OrderingTransportFileName",
        "ContentConstants.LostLuggageFileName",
        "ContentConstants.FirstMeetingFileName",
        "ContentConstants.DailyStandupFileName",
        "ContentConstants.PhoneCallWithAClientFileName",
        "ContentConstants.WorkAskingForClarificationFileName",
        "ContentConstants.DiscussingDeadlinesFileName",
        "ContentConstants.TellMeAboutYourselfFileName",
        "ContentConstants.WorkExperienceFileName",
        "ContentConstants.StrengthsAndWeaknessesFileName",
        "ContentConstants.WhyDoYouWantThisJobFileName",
        "ContentConstants.AskingQuestionsAtTheEndFileName",
        "ContentConstants.BookingATableFileName",
        "ContentConstants.OrderingFoodFileName",
        "ContentConstants.AskingAboutIngredientsFileName",
        "ContentConstants.HandlingAWrongOrderFileName",
        "ContentConstants.PayingTheBillFileName",
        "ContentConstants.OpenConversationFileName",
    ]
    checks = {
        REPO_ROOT / "ViewModels" / "HomeViewModel.cs": [registry["topic"] for registry in EXPECTED_REGISTRY.values()],
        REPO_ROOT / "ViewModels" / "SubtopicsViewModel.cs": [
            subtopic for registry in EXPECTED_REGISTRY.values() for subtopic in registry["files"].values()
        ],
        REPO_ROOT / "ViewModels" / "MainViewModel.cs": topic_folder_constants + lesson_file_constants,
        REPO_ROOT / "Constants" / "ContentConstants.cs": list(EXPECTED_REGISTRY.keys()) + [
            file_name for registry in EXPECTED_REGISTRY.values() for file_name in registry["files"].keys()
        ],
    }

    for path, needles in checks.items():
        if not path.exists():
            report.error(f"Routing source file is missing: {rel(path)}")
            continue
        text = path.read_text(encoding="utf-8")
        for needle in needles:
            if needle not in text:
                report.error(f"Routing source check failed: {rel(path)} does not contain {needle!r}")


def print_report(report: AuditReport, parsed_count: int) -> None:
    print("Lesson content audit")
    print("====================")
    print(f"Repository: {REPO_ROOT}")
    print(f"Lesson JSON files parsed: {parsed_count}")
    print()

    if report.errors:
        print("Errors:")
        for message in report.errors:
            print(f"  [ERROR] {message}")
        print()

    if report.warnings:
        print("Warnings:")
        for message in report.warnings:
            print(f"  [WARN] {message}")
        print()

    if report.infos:
        print("Info:")
        for message in report.infos:
            print(f"  [INFO] {message}")
        print()

    if report.errors:
        print(f"FAILED: {len(report.errors)} error(s), {len(report.warnings)} warning(s).")
    else:
        print(f"PASSED: 0 errors, {len(report.warnings)} warning(s).")


def main() -> int:
    report = AuditReport()
    parsed = load_json_files(report)
    check_obsolete_folders(report)
    check_folders_and_registry(parsed, report)
    check_text_content(report)
    check_required_fields(parsed, report)
    check_routing_sources(report)
    print_report(report, len(parsed))
    return 1 if report.errors else 0


if __name__ == "__main__":
    sys.exit(main())
