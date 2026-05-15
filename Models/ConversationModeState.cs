namespace EnglishVoiceTutor.Desktop.Models;

public enum ConversationModeState
{
    NotStarted,
    Starting,
    Ready,
    Recording,
    WaitingForTranscript,
    WaitingForAssistant,
    PlayingAssistantAudio,
    Stopping,
    Faulted,
    CompletedAwaitingFinish
}
