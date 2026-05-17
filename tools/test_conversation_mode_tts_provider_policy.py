from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, path: str) -> None:
    require(needle in text, f"Missing {needle!r} in {path}")


def method_body(text: str, method_name: str) -> str:
    markers = [f"private async Task {method_name}(" , f"private Task {method_name}(", f"{method_name}("]
    start = -1
    for marker in markers:
        start = text.find(marker)
        if start >= 0:
            break
    require(start >= 0, f"Missing method {method_name}")
    brace_start = text.find("{", start)
    depth = 0
    for index in range(brace_start, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[brace_start:index + 1]
    raise AssertionError(f"Could not parse method {method_name}")


def main() -> None:
    vm_path = "ViewModels/LessonChatViewModel.cs"
    backend_constants_path = "Constants/BackendConstants.cs"
    service_path = "Services/LessonChatBackendService.cs"
    backend_speech_path = "backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs"
    backend_request_path = "backend/EnglishVoiceTutor.Api/Models/AudioSpeechRequest.cs"
    client_request_path = "Models/AudioSpeechBackendRequest.cs"
    provider_path = "Models/ConversationModeVoiceProvider.cs"

    vm = read(vm_path)
    constants = read(backend_constants_path)
    service = read(service_path)
    backend_speech = read(backend_speech_path)
    backend_request = read(backend_request_path)
    client_request = read(client_request_path)
    provider = read(provider_path)

    require_text(provider, "enum ConversationModeVoiceProvider", provider_path)
    require_text(provider, "Tts1", provider_path)
    require_text(provider, "Realtime", provider_path)
    require_text(constants, 'DefaultConversationModeVoiceProvider = "Tts1"', backend_constants_path)
    require_text(vm, "ResolveConversationModeVoiceProvider", vm_path)
    require_text(vm, "StartConversationModeAsync", vm_path)
    require_text(vm, "StartTtsConversationModeAsync", vm_path)
    require_text(vm, "StartRealtimeConversationModeAsync", vm_path)

    start_tts_body = method_body(vm, "StartTtsConversationModeAsync")
    require("StartRealtimeConversationAsync" not in start_tts_body, "Default TTS start must not call StartRealtimeConversationAsync")
    require("EnsureRealtimeSessionStartedAsync" not in start_tts_body, "Default TTS start must not create a Realtime session")
    require("CreateRealtimeVoiceWebSocketUri" not in start_tts_body, "Default TTS start must not build/open /api/realtime-voice WebSocket")
    require("RealtimeVoiceEndpoint" not in start_tts_body and "/api/realtime-voice" not in start_tts_body, "Default TTS start must not reference /api/realtime-voice")

    require_text(vm, "SelectCurrentConversationOpeningBotMessage", vm_path)
    require_text(vm, "ConversationLatestBotText = openingBotMessage.Text", vm_path)
    require_text(vm, "PlayConversationModeBotVoiceAsync(openingBotMessage, isOpeningPlayback: true)", vm_path)
    require_text(vm, "ConversationModeState.OpeningPlayback", vm_path)
    require_text(vm, "tts_opening_bot_voice_playback_finished", vm_path)
    require_text(vm, "VoicePlaybackUnavailableMessage", vm_path)

    start_tts_body = method_body(vm, "StartTtsConversationModeAsync")
    require("AddMessage(" not in start_tts_body, "TTS Conversation Mode entry must not duplicate the visible bot message in chat history")
    require("PlayConversationModeBotVoiceAsync(openingBotMessage, isOpeningPlayback: true)" in start_tts_body, "TTS Conversation Mode entry must speak the current/latest bot message")
    require("BackendConstants.ConversationModeTtsPurpose" in start_tts_body, "TTS Conversation Mode entry must log/use the Conversation Mode TTS purpose")
    require("ConversationModeTtsSpeechSpeed" in start_tts_body, "TTS Conversation Mode entry must use/log the named Conversation Mode speed")
    require_text(vm, "StartRealtimeConversationModeAsync", vm_path)
    require_text(vm, "EnsureRealtimeSessionStartedAsync", vm_path)

    require_text(service, "SendAudioForTranscriptionAsync", service_path)
    require_text(constants, "AudioTranscriptionEndpoint", backend_constants_path)
    require_text(service, "SendLessonMessageAsync", service_path)
    require_text(constants, "LessonChatReplyEndpoint", backend_constants_path)
    require_text(service, "CreateBotSpeechAsync", service_path)
    require_text(constants, "AudioSpeechEndpoint", backend_constants_path)
    require_text(constants, 'LessonChatTtsModel = "tts-1"', backend_constants_path)
    require_text(constants, 'ConversationModeTtsModel = "gpt-4o-mini-tts"', backend_constants_path)
    require_text(constants, 'ConversationModeTtsPurpose = "conversation_mode_tts"', backend_constants_path)
    require_text(vm, "BackendConstants.ConversationModeTtsPurpose", vm_path)
    require_text(service, "SpeechSpeed = speechSpeed", service_path)
    require_text(service, "Instructions = instructionsToSend", service_path)
    require_text(client_request, "SpeechSpeed", client_request_path)
    require_text(backend_request, "SpeechSpeed", backend_request_path)
    require_text(backend_request, "Instructions", backend_request_path)
    require_text(backend_speech, "ConversationModeTtsPurpose", backend_speech_path)
    require_text(backend_speech, "ResolveSpeechSpeed", backend_speech_path)

    speed_match = re.search(r"ConversationModeTtsSpeechSpeed\s*=\s*([0-9.]+)", constants)
    require(speed_match is not None, "Conversation Mode TTS speed must be a named constant")
    require(float(speed_match.group(1)) == 1.0, "Conversation Mode TTS speed should start at 1.0 for gpt-4o-mini-tts testing")
    require_text(vm, "ConversationModeTtsSpeechSpeed", vm_path)

    require_text(vm, "CancelCurrentBotVoice(BotVoiceCancellationReasons.NewerMessageCancel)", vm_path)
    require_text(vm, "botVoiceSemaphore", vm_path)
    require_text(vm, "IsBotVoicePlaying", vm_path)
    require_text(vm, "PlayConversationModeBotVoiceAsync", vm_path)
    require_text(vm, "!IsConversationModeEnabled && !IsRealtimeConversationActive && !IsLessonCompleteAwaitingFinish && IsBotVoiceAutoPlayEnabled", vm_path)
    require_text(vm, "Skipping normal bot voice", vm_path)
    require_text(vm, "return !IsConversationModeEnabled && !IsRealtimeConversationActive && message.ShowPlayVoiceButton", vm_path)
    require_text(vm, "speechPurpose: BackendConstants.ConversationModeTtsPurpose", vm_path)
    require_text(vm, "speechSpeed: ConversationModeTtsSpeechSpeed", vm_path)
    require_text(vm, "speechModel: BackendConstants.ConversationModeTtsModel", vm_path)
    require_text(vm, "speechInstructions: BuildConversationModeTtsInstructions()", vm_path)
    require_text(vm, "allowDuringRealtimeOpeningPlayback: false", vm_path)
    require_text(vm, "tts_context_selected_waiting_for_opening_voice", vm_path)
    require_text(vm, "await PlayConversationModeBotVoiceAsync(roleplayStartMessage)", vm_path)
    require_text(vm, "VoicePlaybackUnavailableMessage", vm_path)

    require_text(vm, "ConversationLatestUserText = trimmedTranscriptionText", vm_path)
    require_text(vm, "ConversationLatestBotText = botReply", vm_path)
    require_text(vm, "AddLearnerMessage(userMessage, messageSource, nextLearnerTurnCount, mappedFeedback)", vm_path)
    require_text(vm, "isFeedbackEligible: true", vm_path)
    require_text(vm, "CountsAsValidLessonTurn", vm_path)
    require_text(vm, "BuildLessonSummaryInput", vm_path)

    lesson_json_changed = any(path.suffix.lower() == ".json" and "lesson" in str(path).lower() for path in ROOT.glob("**/*.json") if ".git" in path.parts)
    require(not lesson_json_changed, "Policy sanity check failed while scanning lesson JSON")

    print("Conversation Mode TTS provider policy checks passed.")


if __name__ == "__main__":
    main()
