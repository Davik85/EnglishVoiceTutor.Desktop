from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


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
        assert phrase not in builder

    assert "Correct softly and only when needed during roleplay." in base_prompt
    assert "Use alternative phrasing" in response_rules
    assert "only for an error, unnatural wording, learner help request, or explicit model-phrase teaching mode" in response_rules
    assert "If the learner answer is acceptable, acknowledge briefly and continue the scenario without correction advice." in response_rules


def test_prompt_builder_assembles_cms_prompt_templates_from_runtime_request() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    backend_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    runtime_endpoint = read("backend/EnglishVoiceTutor.Api/Program.cs")
    desktop_viewmodel = read("ViewModels/LessonChatViewModel.cs")

    assert "AppendCmsPromptTemplates(prompt, request)" in builder
    assert "CmsContentConstants.PromptTemplateKeys.LessonTutorBase" in builder
    assert "CmsContentConstants.PromptTemplateKeys.LessonResponseRules" in builder
    assert "public IReadOnlyDictionary<string, string> PromptTemplates" in backend_request
    assert "public IReadOnlyDictionary<string, string> PromptTemplates" in desktop_request
    assert "scenario.Lesson.PromptTemplates = result.Content.PromptTemplates" in runtime_endpoint
    assert "PromptTemplates = lessonScenario.PromptTemplates" in desktop_viewmodel


def test_backend_guardrails_are_preserved_without_editable_behavior_policy() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Ask at most one question in a turn." in builder
    assert "Runtime phase controls whether to continue active roleplay, wrap up, or give the final message." in builder
    assert "Do not continue active dialogue after the final phase message." in builder
    assert "Target-language lesson language lock:" in builder
    assert "Ask one question at a time." in response_rules
    assert "Do not define wrap-up or final-message turn numbers in prompt templates." in response_rules


def test_do_not_restart_after_roleplay_seed_is_cms_owned() -> None:
    builder = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    response_rules = read("Content/Prompts/lesson_response_rules.txt")

    assert "Do not restart greeting, setup, or introductions after roleplay has begun." not in builder
    assert "Do not restart greeting, setup, or introductions after roleplay has begun." in response_rules


def test_docs_make_cms_first_behavior_tuning_clear() -> None:
    guide = read("docs/LESSON_BEHAVIOR_TUNING_GUIDE.md")

    assert "normal behavior tuning should be done in CMS, not by editing backend code" in guide
    assert "Do not tune normal tutor behavior by editing LessonPromptBuilder.cs first" in guide
    assert "Correction frequency" in guide
    assert "Natural roleplay behavior" in guide
    assert "Scenario continuity" in guide
    assert "Level strictness" in guide
    assert "Tutor personality" in guide
    assert "Wrap/final wording" in guide


def test_level_profile_timing_source_of_truth_is_preserved() -> None:
    levels = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsLevelProfiles.cs")
    snapshot_builder = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentSnapshotBuilder.cs")

    assert "A1WrapUpAfterUserTurn = 10" in levels
    assert "A1FinalMessageAtUserTurn = 15" in levels
    assert "Published runtime scenarios must not carry independent turn thresholds" in snapshot_builder
    assert "ApplyCmsLevelProfiles" in snapshot_builder
