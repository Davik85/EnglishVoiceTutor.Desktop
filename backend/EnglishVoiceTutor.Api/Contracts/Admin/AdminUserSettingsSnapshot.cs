namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserSettingsSnapshot
{
    public string? StudyLanguage { get; set; }
    public string? ExplanationLanguage { get; set; }
    public string? SpeechVoice { get; set; }
    public decimal? SpeechSpeed { get; set; }
    public bool? ConversationModeEnabled { get; set; }
}
