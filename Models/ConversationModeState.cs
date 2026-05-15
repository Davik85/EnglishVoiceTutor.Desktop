namespace EnglishVoiceTutor.Desktop.Models;

public enum ConversationModeState
{
    NotStarted,
    Starting,
    Ready,
    OpeningPlayback,
    Recording,
    WaitingForTranscript,
    WaitingForAssistant,
    PlayingAssistantAudio,
    Stopping,
    Faulted,
    CompletedAwaitingFinish
}
