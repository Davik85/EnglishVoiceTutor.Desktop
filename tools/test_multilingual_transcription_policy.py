#!/usr/bin/env python3
"""Deterministic checks for selected study-language transcription and TTS wiring."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require_text(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    desktop_service = read("Services/LessonChatBackendService.cs")
    vm = read("ViewModels/LessonChatViewModel.cs")
    api_program = read("backend/EnglishVoiceTutor.Api/Program.cs")
    transcription = read("backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")
    speech_request = read("Models/AudioSpeechBackendRequest.cs")

    for needle in ["targetLanguageId", "targetLanguageName", "targetLanguageNativeName", "targetLanguageCode", "TranscriptionLanguageCode"]:
        require_text(desktop_service + api_program, needle, "transcription request/endpoint")
    require_text(vm, "SendAudioForTranscriptionAsync(savedFilePath, studyLanguage)", "ViewModels/LessonChatViewModel.cs")
    require_text(vm, "SendAudioForTranscriptionAsync(fallbackFilePath, studyLanguage, CancellationToken.None)", "ViewModels/LessonChatViewModel.cs")
    require_text(transcription, "targetLanguage.TranscriptionLanguageCode", "backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")
    require_text(transcription, "The learner is practicing {targetLanguage.EnglishName}", "backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs")

    for needle in ["TargetLanguageId", "TargetLanguageName", "TargetLanguageNativeName", "TargetLanguageCode"]:
        require_text(speech_request, needle, "Models/AudioSpeechBackendRequest.cs")
    require_text(vm, "Speak in a calm, friendly {targetLanguageName} tutor voice", "ViewModels/LessonChatViewModel.cs")
    require_text(desktop_service, "SpeechModelSupportsInstructions(resolvedModel) ? instructions : null", "Services/LessonChatBackendService.cs")

    print("Multilingual transcription policy checks passed.")


if __name__ == "__main__":
    main()
