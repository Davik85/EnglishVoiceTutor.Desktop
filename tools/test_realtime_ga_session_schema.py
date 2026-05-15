#!/usr/bin/env python3
"""Static policy tests for the GA Realtime session.update schema."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs"
CONSTANTS = ROOT / "backend/EnglishVoiceTutor.Api/Constants/OpenAiConstants.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, path: Path) -> None:
    require(needle in text, f"Missing {needle!r} in {path.relative_to(ROOT)}")


def main() -> None:
    service = read(SERVICE)
    constants = read(CONSTANTS)
    combined = constants + "\n" + service

    require_text(constants, 'DefaultRealtimeVoiceModel = "gpt-realtime"', CONSTANTS)
    require_text(constants, 'DefaultRealtimeVoice = "coral"', CONSTANTS)
    require_text(constants, 'RealtimeAudioPcmFormatType = "audio/pcm"', CONSTANTS)
    require_text(constants, 'RealtimeInputAudioSampleRate = 24000', CONSTANTS)
    require_text(constants, 'RealtimeOutputAudioSampleRate = 24000', CONSTANTS)
    require_text(constants, 'RealtimeAudioOutputModality = "audio"', CONSTANTS)
    require_text(constants, 'TranscriptionLanguage = "en"', CONSTANTS)

    require_text(service, 'type = "session.update"', SERVICE)
    require_text(service, 'model = OpenAiConstants.DefaultRealtimeVoiceModel', SERVICE)
    require_text(service, 'output_modalities = new[] { OpenAiConstants.RealtimeAudioOutputModality }', SERVICE)
    require_text(service, 'voice = OpenAiConstants.DefaultRealtimeVoice', SERVICE)
    require_text(service, 'type = OpenAiConstants.RealtimeAudioPcmFormatType', SERVICE)
    require_text(service, 'rate = OpenAiConstants.RealtimeInputAudioSampleRate', SERVICE)
    require_text(service, 'rate = OpenAiConstants.RealtimeOutputAudioSampleRate', SERVICE)
    require_text(service, 'language = OpenAiConstants.TranscriptionLanguage', SERVICE)
    require_text(service, 'model = OpenAiConstants.DefaultTranscriptionModel', SERVICE)
    require_text(service, 'Realtime session.update sanitized shape', SERVICE)

    session_update_block = service.split("private static object CreateRealtimeSessionUpdateEvent", 1)[1].split("private void LogRealtimeSessionUpdateShape", 1)[0]
    forbidden_properties = [
        "OpenAI-Beta",
        "input_audio_format",
        "output_audio_format",
        "input_audio_transcription",
    ]
    require("OpenAI-Beta" not in combined, "Forbidden beta Realtime header remains: OpenAI-Beta")
    for forbidden in forbidden_properties[1:]:
        require(forbidden not in session_update_block, f"Forbidden beta Realtime session.update field remains: {forbidden}")

    require(
        not re.search(r'(?<!output_)modalities\s*=', session_update_block) and '"modalities"' not in session_update_block,
        "Found beta `modalities` field instead of GA `output_modalities`.",
    )
    require(
        re.search(r'ConnectAsync\(new Uri\(RealtimeWebSocketEndpoint\)', service),
        "Realtime WebSocket endpoint should be centralized and used for upstream connection.",
    )
    require_text(service, '"wss://api.openai.com/v1/realtime?model="', SERVICE)

    print("Realtime GA session schema policy passed.")


if __name__ == "__main__":
    main()
