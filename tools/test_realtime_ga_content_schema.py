#!/usr/bin/env python3
"""Static policy tests for GA Realtime conversation item and response content schemas."""
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


def block_between(text: str, start: str, end: str) -> str:
    require(start in text, f"Missing block start {start!r}")
    remainder = text.split(start, 1)[1]
    require(end in remainder, f"Missing block end {end!r}")
    return remainder.split(end, 1)[0]


def main() -> None:
    service = read(SERVICE)
    constants = read(CONSTANTS)

    for needle in [
        'RealtimeConversationItemCreateEventType = "conversation.item.create"',
        'RealtimeResponseCreateEventType = "response.create"',
        'RealtimeInputTextContentType = "input_text"',
        'RealtimeOutputTextContentType = "output_text"',
        'RealtimeInputAudioContentType = "input_audio"',
        'RealtimeOutputAudioContentType = "output_audio"',
        'RealtimeAudioOutputModality = "audio"',
    ]:
        require_text(constants, needle, CONSTANTS)

    require(not re.search(r'type\s*=\s*"text"', service), 'GA Realtime must not serialize a conversation content part with type = "text".')
    require(not re.search(r'new\s*\{\s*type\s*=\s*"text"', service, re.DOTALL), 'GA Realtime must not create anonymous content part type "text".')

    seed_block = block_between(service, 'private async Task SeedRecentConversationAsync', 'private static string GetRealtimeTextContentTypeForRole')
    require_text(seed_block, 'role = IsTutorSender(message, request) ? "assistant" : "user"', SERVICE)
    require_text(seed_block, 'type = GetRealtimeTextContentTypeForRole(role)', SERVICE)

    role_mapping = block_between(service, 'private static string GetRealtimeTextContentTypeForRole', 'private string BuildResponseInstructions')
    require_text(role_mapping, 'OpenAiConstants.RealtimeOutputTextContentType', SERVICE)
    require_text(role_mapping, 'OpenAiConstants.RealtimeInputTextContentType', SERVICE)
    require_text(role_mapping, 'role.Equals("assistant"', SERVICE)

    user_text_block = service.split('case "user.text":', 1)[1].split('case "user.audio.start":', 1)[0]
    require_text(user_text_block, 'OpenAiConstants.RealtimeConversationItemCreateEventType', SERVICE)
    require_text(user_text_block, 'role = "user"', SERVICE)
    require_text(user_text_block, 'OpenAiConstants.RealtimeInputTextContentType', SERVICE)

    response_block = block_between(service, 'private async Task CreateResponseAsync', 'private void LogRealtimeResponseUsage')
    require_text(response_block, 'OpenAiConstants.RealtimeResponseCreateEventType', SERVICE)
    require_text(response_block, 'output_modalities = new[] { OpenAiConstants.RealtimeAudioOutputModality }', SERVICE)
    require_text(response_block, 'instructions = BuildResponseInstructions()', SERVICE)
    require('content = new[]' not in response_block, 'response.create must not fake content items for per-response instructions.')
    require('type = "text"' not in response_block, 'response.create must not include invalid content part type "text".')

    corrective_block = block_between(service, 'private string BuildCorrectiveEnglishOnlyInstructions', 'private async Task CreateResponseAsync')
    require('type = "text"' not in corrective_block, 'English-only corrective Realtime event must not include invalid content part type "text".')
    require_text(service, 'Realtime outbound event shape', SERVICE)
    require_text(service, 'ContentPartTypes={ContentPartTypes}', SERVICE)
    require_text(service, 'OutputModalities={OutputModalities}', SERVICE)
    require_text(service, 'session.runtime_failed', SERVICE)
    require_text(service, 'upstream_realtime_error', SERVICE)

    print('Realtime GA content schema policy passed.')


if __name__ == "__main__":
    main()
