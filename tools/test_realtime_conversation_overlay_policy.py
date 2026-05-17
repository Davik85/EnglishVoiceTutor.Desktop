from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEW = ROOT / "Views" / "LessonChatView.xaml"
VM = ROOT / "ViewModels" / "LessonChatViewModel.cs"

view_text = VIEW.read_text(encoding="utf-8")
vm_text = VM.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def command_body(command_name: str) -> str:
    marker = f"private async Task {command_name}Async()"
    start = vm_text.find(marker)
    require(start >= 0, f"Missing {command_name}Async command body")
    brace_start = vm_text.find("{", start)
    depth = 0
    for index in range(brace_start, len(vm_text)):
        char = vm_text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return vm_text[brace_start:index + 1]
    raise AssertionError(f"Could not parse {command_name}Async command body")


conversation_hint_body = command_body("ConversationHint")

require('Content="Hint"' in view_text, "Conversation Mode overlay must include a visible Hint button label")
require('Command="{Binding ConversationHintCommand}"' in view_text, "Conversation Hint button must bind to ConversationHintCommand")
require('ConversationLatestUserText' in view_text and 'HasConversationLatestUserText' in view_text, "Conversation overlay must display latest user transcript only")
require('ConversationLatestBotText' in view_text and 'HasConversationLatestBotText' in view_text, "Conversation overlay must display latest bot phrase only")
require('ItemsSource="{Binding Messages}"' not in view_text[view_text.find('Command="{Binding ConversationHintCommand}"') - 4000:view_text.find('Command="{Binding ConversationHintCommand}"') + 4000], "Conversation overlay must not render full chat history")
require('ConversationHintText' in view_text and 'HasConversationHintText' in view_text, "Conversation hint must render from dedicated conversation hint state")

require('ConversationLatestUserText' in vm_text, "View model must expose dedicated latest user overlay text")
require('ConversationLatestBotText' in vm_text, "View model must expose dedicated latest bot overlay text")
require('ConversationHintText' in vm_text and 'IsConversationHintVisible' in vm_text, "View model must keep conversation hint state separate from normal CurrentHintText")
require('CurrentHintText' not in conversation_hint_body, "Conversation Hint command must not show the normal lesson-chat hint card")
require('StopRealtimeConversationAsync' not in conversation_hint_body, "Conversation Hint command must not stop realtime conversation")
require('StopSessionAsync' not in conversation_hint_body and 'SendUserTextAsync' not in conversation_hint_body and 'CommitUserAudioAsync' not in conversation_hint_body, "Conversation Hint command must not send realtime stop/cancel/user-turn operations")
require('IsConversationHintVisible = false' in conversation_hint_body, "Conversation Hint command must hide the hint when toggled again")
require('HideConversationHint();' in vm_text[vm_text.find('StartRealtimeVoiceRecordingAsync'):vm_text.find('StopRealtimeVoiceRecordingAsync')], "Conversation hint must hide on record start")
require('ClearConversationOverlayState(clearPhrases: true);' in command_body('ToggleConversationMode'), "Conversation hint must clear on Conversation Mode exit/back button")
require('HideConversationHint();' in vm_text[vm_text.rfind('OnRealtimeUserTranscriptDeltaReceived'):vm_text.rfind('OnRealtimeUserTranscriptCompleted')], "Conversation hint must hide when user transcript arrives")
require('HideConversationHint();' in vm_text[vm_text.rfind('OnRealtimeAssistantTranscriptDeltaReceived'):vm_text.rfind('OnRealtimeAssistantTurnCompleted')], "Conversation hint must hide when bot transcript arrives")

print("Realtime conversation overlay policy checks passed.")
