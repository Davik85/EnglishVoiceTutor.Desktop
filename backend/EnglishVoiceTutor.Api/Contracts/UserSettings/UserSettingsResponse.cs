namespace EnglishVoiceTutor.Api.Contracts.UserSettings;

public sealed record UserSettingsResponse(
    Guid UserId,
    string StudyLanguage,
    string ExplanationLanguage,
    string SpeechVoice,
    decimal SpeechSpeed,
    bool ConversationModeEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
