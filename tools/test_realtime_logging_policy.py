#!/usr/bin/env python3
"""Static policy tests for Realtime Conversation Mode logging."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str) -> None:
    require(needle in text, f"Missing {needle!r} in {SERVICE.relative_to(ROOT)}")


def block_between(text: str, start: str, end: str) -> str:
    require(start in text, f"Missing start marker {start!r}")
    tail = text.split(start, 1)[1]
    require(end in tail, f"Missing end marker {end!r}")
    return tail.split(end, 1)[0]


def main() -> None:
    service = read(SERVICE)

    append_block = block_between(service, 'case "user.audio.append":', 'case "user.audio.commit":')
    require('logger.LogInformation("Realtime desktop audio append received' not in append_block,
            "Per-chunk desktop audio append logging must not be Information-level.")
    require('logger.LogDebug("Realtime desktop audio append received' in append_block,
            "Per-chunk desktop audio append logging should be Debug-level.")
    require('AudioAppendDebugLogEveryChunkCount' in append_block,
            "Nth-chunk Debug logging must use a named constant.")
    require('Realtime first audio chunk received' in append_block,
            "First audio chunk should remain visible at Information level.")

    outbound_shape_block = block_between(service, 'private void LogRealtimeOutboundEventShape', 'private string DetermineRealtimeOutboundItemKind')
    require('logger.LogDebug("Realtime outbound event shape' in outbound_shape_block,
            "Outbound event shape logging should be Debug-level, not per-chunk Information-level.")
    require('logger.LogInformation("Realtime outbound event shape' not in outbound_shape_block,
            "Outbound event shape logging must not be Information-level.")

    for needle in [
        'Realtime desktop start_recording event received',
        'Realtime desktop commit received',
        'AudioChunkCount={AudioChunkCount}',
        'BufferedBytes={BufferedBytes}',
        'EstimatedBufferedAudioDurationSeconds={EstimatedBufferedAudioDurationSeconds}',
        'TimeFromStartRecordingToCommitMs={TimeFromStartRecordingToCommitMs}',
        'Realtime user transcript completed received',
        'Realtime user transcript accepted',
        'Realtime response.create sent',
        'Realtime first assistant audio delta ms',
        'Realtime first assistant transcript delta ms',
        'Realtime assistant response completed ms',
        'DesktopStopReason={DesktopStopReason}',
    ]:
        require_text(service, needle)

    require_text(service, 'currentRealtimeUserTurnId = CreateRealtimeUserTurnId();')
    require_text(service, 'RealtimeUserTurnId={RealtimeUserTurnId}')
    require_text(service, 'Operation=realtime_response; SessionId={SessionId}; RealtimeUserTurnId={RealtimeUserTurnId}; LearnerTurnNumber={LearnerTurnNumber}; ResponseId={ResponseId}')

    require_text(service, 'private static string ExtractRealtimeResponseId(JsonElement root)')
    require_text(service, 'root.TryGetProperty("response_id"')
    require_text(service, 'responseProperty.TryGetProperty("id"')
    require_text(service, 'private const string UnknownResponseId = "unknown";')
    require_text(service, 'GetLogResponseId(responseId)')
    require_text(service, 'Realtime multiple active response ids detected')
    response_created_block = block_between(service, 'case "response.created":', 'case "response.output_audio.delta":')
    require('GetLogResponseId(responseId)' in response_created_block,
            "response.created must log unknown for missing ids instead of reusing a stale active id.")
    require('activeResponseId' not in response_created_block.split('logger.LogInformation', 1)[1],
            "response.created log should not use activeResponseId directly.")

    require_text(service, 'Threshold={Threshold}; SessionId={SessionId}; LessonType={LessonType}; Level={Level}; Topic={Topic}; Subtopic={Subtopic}; LearnerTurnCount={LearnerTurnCount}; SoftWrapUpAfterUserTurn={SoftWrapUpAfterUserTurn}; FinalMessageAtUserTurn={FinalMessageAtUserTurn}; IsFinalTurn={IsFinalTurn}; ShouldDisableFurtherInput={ShouldDisableFurtherInput}')
    require_text(service, 'LogLessonTurnThreshold("SoftWrapUpStarted", isFinalTurn: false)')
    require_text(service, 'LogLessonTurnThreshold("FinalMessageRequired", isFinalTurn: true)')

    forbidden_secret_patterns = [r'sk-[A-Za-z0-9]', r'OPENAI_API_KEY\s*=', r'api[_-]?key\s*[:=]']
    lowered = service.lower()
    require('transcript = validation.NormalizedTranscript' in service,
            "Desktop transcript delivery may include transcript, but backend logs should not log raw transcript text.")
    for pattern in forbidden_secret_patterns:
        require(re.search(pattern, service, flags=re.IGNORECASE) is None,
                f"Potential secret logging or fixture found: {pattern}")
    require('Transcript={Transcript}' not in service and 'UserTranscript={UserTranscript}' not in service,
            "Raw user transcript text must not be logged.")

    print("Realtime logging policy checks passed.")


if __name__ == "__main__":
    main()
