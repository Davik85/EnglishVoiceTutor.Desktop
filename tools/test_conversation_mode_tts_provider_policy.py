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
    declaration = re.search(
        r"(?:public|private|static)\s+(?:(?:static|async)\s+)*[\w<>?\[\]]+\s+"
        + re.escape(method_name) + r"\s*\(", text)
    require(declaration is not None, f"Missing method {method_name}")
    brace_start = text.find("{", declaration.end())
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


def require_speech_routing(constants: str, backend_speech: str, program: str) -> None:
    require('DefaultConversationModeVoiceProvider = "Tts1"' in constants, "TTS must remain the default")
    for name, description in [
        ("LessonChatTtsModel", "backend-configured lesson chat TTS model"),
        ("ConversationModeTtsModel", "backend-configured conversation TTS model"),
    ]:
        require(f'{name} = "{description}"' in constants, f"{name} must describe its backend-configured role")

    resolver = method_body(backend_speech, "ResolveSpeechModel")
    require("purpose, ConversationModeTtsPurpose" in resolver, "TTS model selection must check purpose")
    require("return modelSettings.ConversationModeTextToSpeechModel;" in resolver, "Conversation TTS must use active settings")
    require("return modelSettings.LessonChatTextToSpeechModel;" in resolver, "Lesson Chat TTS must use active settings")
    for name in ["CreateSpeechAsync", "StreamSpeechAsync"]:
        body = method_body(backend_speech, name)
        for expression in [
            "NormalizePurpose(purpose)", "ResolveSpeechSpeed(normalizedPurpose, speechSpeed)",
            "_aiModelSettingsService.GetActiveSettings()", "ResolveSpeechModel(normalizedPurpose, modelSettings)",
            "ResolveSpeechInstructions(resolvedModel, instructions)", "Model = resolvedModel",
            "Voice = ResolveSpeechVoice(speechVoice)", "Instructions = resolvedInstructions",
            "Speed = resolvedSpeechSpeed", "ResolveStudyLanguage(targetLanguageName, targetLanguageId)",
        ]:
            require(expression in body, f"{name} must preserve {expression}")
        require("Model = model" not in body, f"{name} must not use the client's descriptive model")

    speech_endpoint = method_body(program, "HandleAudioSpeechAsync")
    stream_endpoint = method_body(program, "HandleAudioSpeechStreamAsync")
    for name, method, body in [
        ("speech", "CreateSpeechAsync", speech_endpoint),
        ("speech-stream", "StreamSpeechAsync", stream_endpoint),
    ]:
        call = re.search(r"audioSpeechService\." + method + r"\((.*?)\);", body, re.DOTALL)
        require(call is not None, f"{name} must call {method}")
        for field in ["Purpose", "SpeechSpeed", "Instructions", "SpeechVoice", "TargetLanguageName", "TargetLanguageId"]:
            require(f"request.{field}" in call.group(1), f"{name} must forward {field}")
    require("request.Model" not in stream_endpoint, "Stream model selection must remain backend-authoritative")
    transport = method_body(backend_speech, "StreamAudioSpeechRequestAsync")
    require("HttpMethod.Post, OpenAiConstants.AudioSpeechEndpoint" in transport, "Streaming must retain HTTP speech transport")
    require("HttpCompletionOption.ResponseHeadersRead" in transport, "Streaming must retain incremental reads")
    require("ResponseFormat = OpenAiConstants.DefaultBotVoiceStreamResponseFormat" in method_body(backend_speech, "StreamSpeechAsync"), "Streaming must retain its audio format")


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
    require_speech_routing(constants, backend_speech, read("backend/EnglishVoiceTutor.Api/Program.cs"))
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
    require(float(speed_match.group(1)) == 1.0, "Conversation Mode TTS default speed must remain 1.0")
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
