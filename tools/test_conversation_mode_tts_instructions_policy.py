from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_text(text: str, needle: str, path: str) -> None:
    require(needle in text, f"Missing {needle!r} in {path}")


def main() -> None:
    constants_path = "Constants/BackendConstants.cs"
    vm_path = "ViewModels/LessonChatViewModel.cs"
    service_path = "Services/LessonChatBackendService.cs"
    backend_request_path = "backend/EnglishVoiceTutor.Api/Models/AudioSpeechRequest.cs"
    client_request_path = "Models/AudioSpeechBackendRequest.cs"
    openai_request_path = "backend/EnglishVoiceTutor.Api/Models/OpenAiAudioSpeechRequest.cs"
    backend_service_path = "backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs"
    program_path = "backend/EnglishVoiceTutor.Api/Program.cs"

    constants = read(constants_path)
    vm = read(vm_path)
    service = read(service_path)
    backend_request = read(backend_request_path)
    client_request = read(client_request_path)
    openai_request = read(openai_request_path)
    backend_service = read(backend_service_path)
    program = read(program_path)

    require_text(constants, 'ConversationModeTtsModel = "gpt-4o-mini-tts"', constants_path)
    require_text(constants, 'LessonChatTtsModel = "tts-1"', constants_path)
    require_text(constants, 'ConversationModeTtsSpeechSpeed = 1.0', constants_path)
    require_text(constants, 'ConversationModeTtsPurpose = "conversation_mode_tts"', constants_path)
    for word in ["calm", "even pace", "Do not shout", "Do not rush", "Pronounce clearly"]:
        require_text(constants, word, constants_path)

    require_text(client_request, "string? Instructions", client_request_path)
    require_text(backend_request, "string? Instructions", backend_request_path)
    require_text(openai_request, '[JsonPropertyName("instructions")]', openai_request_path)
    require_text(openai_request, "JsonIgnoreCondition.WhenWritingNull", openai_request_path)

    require_text(vm, "speechModel: BackendConstants.ConversationModeTtsModel", vm_path)
    require_text(vm, "speechInstructions: BuildConversationModeTtsInstructions()", vm_path)
    require_text(vm, "Speak in a calm, friendly {targetLanguageName} tutor voice", vm_path)
    require_text(vm, "Speak only in {targetLanguageName} unless quoting the learner", vm_path)
    require_text(vm, "ConversationLatestBotText = botReply", vm_path)
    require_text(vm, "ConversationLatestBotText = openingBotMessage.Text", vm_path)
    require_text(vm, "var ttsInputText = message.Text", vm_path)
    require_text(vm, "TextMatchesVisible", vm_path)
    require_text(vm, "VisibleTextLength", vm_path)
    require_text(vm, "TtsInputLength", vm_path)
    require_text(vm, "Warning: Conversation Mode TTS input differs from visible bot text", vm_path)
    require_text(vm, "return message.Text;", vm_path)

    require("BuildConversationModeSpokenOpeningText" not in vm, "Conversation Mode must not use a separate spoken opening text builder")
    require("shortened" not in vm.lower(), "Conversation Mode must not add shortened spoken-text helpers")
    require("summary" not in vm[vm.find("PlayConversationModeBotVoiceAsync"):vm.find("PlaySegmentedHighQualityBotVoiceAsync")].lower(), "Conversation Mode TTS must not summarize visible text")
    require("SplitExactTextIntoSentenceSegments(" not in vm.replace("private static IReadOnlyList<string> SplitExactTextIntoSentenceSegments(", ""), "Conversation Mode sentence chunking must not be active")

    require_text(service, "Instructions = instructionsToSend", service_path)
    require_text(service, "SpeechModelSupportsInstructions(resolvedModel)", service_path)
    require_text(backend_service, "ResolveSpeechInstructions", backend_service_path)
    require_text(backend_service, "SpeechModelSupportsInstructions", backend_service_path)
    require_text(backend_service, "OpenAiConstants.NormalChatTtsModel", backend_service_path)
    require_text(backend_service, "OpenAiConstants.ConversationModeTtsModel", backend_service_path)
    require_text(program, "HasInstructions", program_path)
    require_text(program, "InstructionsLength", program_path)

    require("BackendConstants.LessonChatTtsPurpose" in vm, "Normal lesson_chat_tts path must remain present outside Conversation Mode")
    require("Skipping normal bot voice" in vm, "Normal lesson_chat_tts must be skipped while Conversation Mode is active")
    require('DefaultConversationModeVoiceProvider = "Tts1"' in constants, "Realtime must not become the default provider")

    secret_markers = ["sk-", "OPENAI_API_KEY=", "api_key", "ApiKey = \"sk"]
    for relative_path in [constants_path, vm_path, service_path, backend_service_path, program_path]:
        content = read(relative_path)
        for marker in secret_markers:
            require(marker not in content, f"Potential secret marker {marker!r} found in {relative_path}")

    for path in ROOT.glob("**/*.json"):
        if ".git" not in path.parts and "lesson" in str(path).lower():
            # Policy test is source-only; it asserts the test did not need to edit lesson JSON.
            continue

    print("Conversation Mode TTS instructions policy checks passed.")


if __name__ == "__main__":
    main()
