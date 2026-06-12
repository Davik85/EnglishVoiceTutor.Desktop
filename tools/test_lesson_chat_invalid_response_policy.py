#!/usr/bin/env python3
"""Policy checks for robust lesson chat provider response handling."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "OpenAiLessonChatService.cs"
RESPONSE_MODEL = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Models" / "OpenAiResponsesResponse.cs"
DESKTOP_PROJECT = ROOT / "EnglishVoiceTutor.Desktop.csproj"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, path: Path) -> None:
    if needle not in text:
        raise AssertionError(f"Missing required text in {path.relative_to(ROOT)}: {needle}")


def forbid(text: str, needle: str, path: Path) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden text in {path.relative_to(ROOT)}: {needle}")


def main() -> None:
    service = read(SERVICE)
    response_model = read(RESPONSE_MODEL)
    desktop_project = read(DESKTOP_PROJECT)

    # The primary OpenAI path must continue to use strict Responses API structured output.
    require(service, "Type = OpenAiConstants.JsonSchemaFormatType", SERVICE)
    require(service, "Strict = true", SERVICE)
    require(service, "Schema = LessonChatResponseSchema", SERVICE)

    # Responses API text extraction must support both top-level output_text and nested output content text.
    require(response_model, '[JsonPropertyName("output_text")]', RESPONSE_MODEL)
    require(response_model, "public string OutputText", RESPONSE_MODEL)
    require(service, "TryExtractOutputText", SERVICE)
    require(service, "response.OutputText", SERVICE)
    require(service, "contentItem.Text", SERVICE)

    # Valid JSON wrapped in markdown fences must be normalized before parsing.
    require(service, "NormalizeJsonOutputText", SERVICE)
    require(service, 'trimmed.StartsWith("```", StringComparison.Ordinal)', SERVICE)
    require(service, 'trimmed.LastIndexOf("```", StringComparison.Ordinal)', SERVICE)

    # Invalid provider output should produce safe validation reasons and retry once before fallback.
    require(service, "LessonChatProviderMaxAttempts = 2", SERVICE)
    require(service, "previousValidationReason", SERVICE)
    require(service, "ValidationReason={ValidationReason}", SERVICE)
    for reason in [
        "empty_output_text",
        "malformed_json",
        "missing_botReply",
        "missing_feedback.shortText",
        "missing_feedback.naturalVersion",
    ]:
        require(service, reason, SERVICE)

    # Fallback keeps the backend response contract valid rather than surfacing a generic 502 for invalid model JSON.
    require(service, "CreateSafeFallbackLessonReply", SERVICE)
    require(service, "SafeFallbackReturned=True", SERVICE)
    require(service, "Sorry, I had trouble creating the next reply", SERVICE)
    require(service, "IsLessonComplete = shouldEndLessonNow", SERVICE)

    # The provider retry/fallback layer must not persist lesson messages or mutate turn counters.
    forbid(service, "LessonMessageService", SERVICE)
    forbid(service, "CreateBackendLessonMessage", SERVICE)
    forbid(service, "UserTurnNumber++", SERVICE)
    forbid(service, "LearnerTurnCount++", SERVICE)

    # The release/tester desktop backend lock must remain unchanged.
    require(desktop_project, "https://api.languagevoicetutor.com", DESKTOP_PROJECT)
    require(desktop_project, "Non-Debug desktop builds are server-only", DESKTOP_PROJECT)

    print("Lesson chat invalid-response policy checks passed.")


if __name__ == "__main__":
    main()
