#!/usr/bin/env python3
"""Deterministic checks for lesson behavior CMS ownership policy.

This script intentionally uses only the Python standard library so the desktop
release gate can run on Windows tester/release machines without requiring
pytest to be installed.
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def require_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Unexpected {label}: {needle}")


def test_editable_behavior_text_lives_in_cms_prompt_seed_not_backend_policy() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    base_prompt = read("Content/Prompts/lesson_tutor_base_prompt.txt")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    editable_phrases = [
        "Do not give alternative phrasing, advice, or model sentences on every turn.",
        "If the learner answer is acceptable or natural enough, briefly acknowledge it and continue with one natural scenario question.",
        "Do not ask again for basic information already answered in recent turns",
        "Do not restart greetings, introductions, setup, context choice, or the opening line after roleplay has begun.",
    ]

    for phrase in editable_phrases:
        require_not_contains(builder, phrase, "CMS-owned editable behavior text in backend policy")

    require_contains(base_prompt, "Correct softly and only when needed during roleplay.", "CMS base correction guidance")
    require_contains(response_rules, "Use alternative phrasing", "CMS response alternative phrasing guidance")
    require_contains(
        response_rules,
        "only for an error, unnatural wording, learner help request, or explicit model-phrase teaching mode",
        "CMS response correction frequency scope",
    )
    require_contains(
        response_rules,
        "If the learner answer is acceptable, acknowledge briefly and continue the scenario without correction advice.",
        "CMS response acceptable-answer handling",
    )
    require_contains(response_rules, "Behave like a conversation partner first during active roleplay.", "CMS roleplay behavior")
    require_contains(
        response_rules,
        "Do not repeat basic questions already answered in recent conversation unless clarification is needed.",
        "CMS scenario continuity repeat guard",
    )


def test_prompt_builder_assembles_cms_prompt_templates_from_runtime_request() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    backend_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    runtime_endpoint = read("backend/EnglishVoiceTutor.Api/Program.cs")
    desktop_viewmodel = read("ViewModels/LessonChatViewModel.cs")

    require_contains(builder, "AppendCmsPromptTemplates(prompt, request)", "runtime CMS prompt template assembly")
    require_contains(builder, "CmsContentConstants.PromptTemplateKeys.LessonTutorBase", "CMS base prompt key assembly")
    require_contains(builder, "CmsContentConstants.PromptTemplateKeys.LessonResponseRules", "CMS response rules key assembly")
    require_contains(backend_request, "public IReadOnlyDictionary<string, string> PromptTemplates", "backend runtime prompt templates request")
    require_contains(desktop_request, "public IReadOnlyDictionary<string, string> PromptTemplates", "desktop runtime prompt templates request")
    require_contains(runtime_endpoint, "scenario.Lesson.PromptTemplates = result.Content.PromptTemplates", "runtime endpoint CMS prompt propagation")
    require_contains(desktop_viewmodel, "PromptTemplates = lessonScenario.PromptTemplates", "desktop request CMS prompt propagation")


def test_backend_guardrails_are_preserved_without_editable_behavior_policy() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    require_contains(builder, "Ask at most one question in a turn.", "backend one-question guardrail")
    require_contains(
        builder,
        "Runtime phase controls whether to continue active roleplay, wrap up, or give the final message.",
        "backend runtime phase guardrail",
    )
    require_contains(builder, "Do not continue active dialogue after the final phase message.", "backend final-phase guardrail")
    require_contains(builder, "Target-language lesson language lock:", "backend language-lock guardrail")
    require_contains(response_rules, "Ask one question at a time.", "CMS one-question response rule")
    require_contains(response_rules, "Do not define wrap-up or final-message turn numbers in prompt templates.", "CMS timing source-of-truth rule")


def test_do_not_restart_after_roleplay_seed_is_cms_owned() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    require_not_contains(
        builder,
        "Do not restart greeting, setup, or introductions after roleplay has begun.",
        "CMS-owned restart guidance in backend policy",
    )
    require_contains(
        response_rules,
        "Do not restart greeting, setup, or introductions after roleplay has begun.",
        "CMS-owned restart guidance",
    )


def test_docs_make_cms_first_behavior_tuning_clear() -> None:
    guide = read("docs/LESSON_BEHAVIOR_TUNING_GUIDE.md")

    require_contains(guide, "normal behavior tuning should be done in CMS, not by editing backend code", "CMS-first tuning overview")
    require_contains(guide, "Do not tune normal tutor behavior by editing LessonPromptBuilder.cs first", "backend-last tuning guidance")
    require_contains(guide, "Correction frequency", "correction-frequency doc coverage")
    require_contains(guide, "Natural roleplay behavior", "natural roleplay doc coverage")
    require_contains(guide, "Scenario continuity", "scenario continuity doc coverage")
    require_contains(guide, "Level strictness", "level strictness doc coverage")
    require_contains(guide, "Tutor personality", "tutor personality doc coverage")
    require_contains(guide, "Wrap/final wording", "wrap/final wording doc coverage")


def test_level_profile_timing_source_of_truth_is_preserved() -> None:
    levels = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsLevelProfiles.cs")
    snapshot_builder = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentSnapshotBuilder.cs")

    require_contains(levels, "A1WrapUpAfterUserTurn = 10", "A1 wrap-up source of truth")
    require_contains(levels, "A1FinalMessageAtUserTurn = 15", "A1 final-message source of truth")
    require_contains(
        snapshot_builder,
        "Published runtime scenarios must not carry independent turn thresholds",
        "runtime scenario timing source-of-truth guard",
    )
    require_contains(snapshot_builder, "ApplyCmsLevelProfiles", "CMS level profile application")


def main() -> int:
    test_editable_behavior_text_lives_in_cms_prompt_seed_not_backend_policy()
    test_prompt_builder_assembles_cms_prompt_templates_from_runtime_request()
    test_backend_guardrails_are_preserved_without_editable_behavior_policy()
    test_do_not_restart_after_roleplay_seed_is_cms_owned()
    test_docs_make_cms_first_behavior_tuning_clear()
    test_level_profile_timing_source_of_truth_is_preserved()
    print("Lesson behavior CMS ownership policy passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
