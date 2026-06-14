namespace EnglishVoiceTutor.Api.Contracts.UserSettings;

public sealed class UpdateUserSettingsRequest
{
    public string NativeLanguage { get; set; } = string.Empty;
    public string StudyLanguage { get; set; } = string.Empty;
    public string ExplanationLanguage { get; set; } = string.Empty;
    public string SpeechVoice { get; set; } = string.Empty;
    public decimal SpeechSpeed { get; set; }
    public bool ConversationModeEnabled { get; set; }
}
