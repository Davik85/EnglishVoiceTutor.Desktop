from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_prompt_policy_rejects_correction_advice_on_every_turn() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    base_prompt = read("Content/Prompts/lesson_tutor_base_prompt.txt")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Do not give alternative phrasing, advice, or model sentences on every turn." in builder
    assert "Correct softly and only when needed during roleplay." in base_prompt
    assert "Use alternative phrasing" in response_rules
    assert "only for an error, unnatural wording, learner help request, or explicit model-phrase teaching mode" in response_rules


def test_acceptable_answers_can_continue_without_correction() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")
    levels = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsLevelProfiles.cs")

    assert "If the learner answer is acceptable or natural enough, briefly acknowledge it and continue with one natural scenario question." in builder
    assert "If the learner answer is acceptable, acknowledge briefly and continue the scenario without correction advice." in response_rules
    assert "If the answer is acceptable, acknowledge and continue." in levels


def test_one_question_at_a_time_policy_is_preserved() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Ask one question at a time." in response_rules
    assert "Ask exactly one scenario-compatible question when continuing active roleplay." in builder
    assert "Ask at most one question in a turn." in builder


def test_do_not_restart_after_roleplay_has_begun() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Never restart the lesson setup during active roleplay." in builder
    assert "Do not restart greetings, introductions, setup, context choice, or the opening line after roleplay has begun." in builder
    assert "Do not restart greeting, setup, or introductions after roleplay has begun." in response_rules


def test_do_not_repeat_answered_basic_questions_from_recent_history() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Track the recent conversation context included in this prompt" in builder
    assert "Do not ask again for basic information already answered in recent turns" in builder
    assert "Do not repeat basic questions already answered in recent conversation unless clarification is needed." in response_rules


def test_level_profile_timing_source_of_truth_is_preserved() -> None:
    levels = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsLevelProfiles.cs")
    snapshot_builder = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentSnapshotBuilder.cs")

    assert "A1WrapUpAfterUserTurn = 10" in levels
    assert "A1FinalMessageAtUserTurn = 15" in levels
    assert "Published runtime scenarios must not carry independent turn thresholds" in snapshot_builder
    assert "ApplyCmsLevelProfiles" in snapshot_builder
