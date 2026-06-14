namespace EnglishVoiceTutor.Desktop.Models;

public sealed class UpdateBackendUserSettingsRequest
{
    public string NativeLanguage { get; init; } = string.Empty;

    public string StudyLanguage { get; init; } = string.Empty;

    public string ExplanationLanguage { get; init; } = string.Empty;

    public string SpeechVoice { get; init; } = string.Empty;

    public decimal SpeechSpeed { get; init; }

    public bool ConversationModeEnabled { get; init; }
}
