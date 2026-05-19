namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendUserSettingsResponse
{
    public Guid UserId { get; set; }

    public string StudyLanguage { get; set; } = string.Empty;

    public string ExplanationLanguage { get; set; } = string.Empty;

    public string SpeechVoice { get; set; } = string.Empty;

    public decimal SpeechSpeed { get; set; }

    public bool ConversationModeEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
