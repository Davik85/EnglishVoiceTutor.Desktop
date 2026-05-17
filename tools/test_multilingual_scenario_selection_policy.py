#!/usr/bin/env python3
"""Regression checks for localized scenario selection and Conversation Mode activation."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require_text(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def require_absent(text: str, needle: str, source: str) -> None:
    if needle in text:
        raise AssertionError(f"Unexpected {needle!r} in {source}")


def main() -> None:
    localized = read("Services/LocalizedLessonTextService.cs")
    vm = read("ViewModels/LessonChatViewModel.cs")
    desktop_request = read("Models/LessonChatBackendRequest.cs")
    api_request = read("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs")
    prompt = read("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")
    summary = read("Models/LessonSummaryInput.cs")

    for phrase in [
        '"meeting a new neighbor" => "Rencontrer un nouveau voisin"',
        '"meeting a new neighbor" => "Conocer a un nuevo vecino"',
        '"meeting a new neighbor" => "Einen neuen Nachbarn treffen"',
        '"meeting a new neighbor" => "Conhecer um novo vizinho"',
        '"meeting a new neighbor" => "Conoscere un nuovo vicino"',
    ]:
        require_text(localized, phrase, "Services/LocalizedLessonTextService.cs")

    for needle in [
        "LocalizedScenarioOption",
        "LocalizedScenarioSelection",
        "GetLocalizedScenarioOptions",
        "TryResolveLocalizedScenarioSelection",
        "int.TryParse(normalizedInput",
        "NormalizeScenarioSelection",
        "ScenarioSelectionMatches",
        "canonicalScenario = option.CanonicalTitle",
        "localizedScenario = option.LocalizedTitle",
    ]:
        require_text(localized, needle, "Services/LocalizedLessonTextService.cs")

    for needle in [
        "FindMatchingContextVariant(userMessage, out var canonicalScenario, out var localizedScenario)",
        "TryResolveLocalizedScenarioSelection(",
        "selectedLocalizedContextTitle = localizedScenario",
        "selectedCustomContextTitle = string.Empty",
        "Localized scenario selection resolved",
        "CountsAsActiveRoleplayTurn=False",
        "StartActiveRoleplayAfterContextSelectionAsync(startMessage, learnerTurnCountBefore)",
        "PhaseTransition SetupContextSelection -> ActiveRoleplay",
        "AddSetupContextLearnerMessage(userMessage, messageSource)",
        "countsAsValidLessonTurn: false",
        "SourceMessageKind = target.SourceMessageKind",
        "PlayConversationModeBotVoiceAsync(redirectMessage)",
        "tts_invalid_context_selection_waiting_for_retry_voice",
        "Skipping normal bot voice",
        "BackendConstants.ConversationModeTtsPurpose",
        "BackendConstants.ConversationModeTtsModel",
        "HasInstructions=True",
    ]:
        require_text(vm, needle, "ViewModels/LessonChatViewModel.cs")

    for source, name in [(desktop_request, "Models/LessonChatBackendRequest.cs"), (api_request, "backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs"), (summary, "Models/LessonSummaryInput.cs")]:
        require_text(source, "SelectedContextTitle", name)
        require_text(source, "SelectedContextLocalizedTitle", name)

    for needle in [
        "Selected roleplay canonical context",
        "Selected roleplay localized display context",
        "The canonical context may be English lesson metadata",
        "The canonical scenario metadata may be in English",
        "All tutor-facing lesson content must be in",
    ]:
        require_text(prompt, needle, "backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs")

    content_root = ROOT / "Content"
    duplicated_language_dirs = [path for path in content_root.rglob("*.json") if any(part in {"fr", "es", "de", "pt", "it"} for part in path.relative_to(content_root).parts)]
    if duplicated_language_dirs:
        raise AssertionError(f"Lesson JSON must not be duplicated per language: {duplicated_language_dirs}")

    require_absent(vm, "lesson_chat_tts autoplay is allowed during Conversation Mode", "ViewModels/LessonChatViewModel.cs")
    print("Multilingual scenario selection policy checks passed.")


if __name__ == "__main__":
    main()
