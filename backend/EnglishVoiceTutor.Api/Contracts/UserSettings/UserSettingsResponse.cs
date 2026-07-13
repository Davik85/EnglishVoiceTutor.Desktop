namespace EnglishVoiceTutor.Api.Contracts.UserSettings;

public sealed record UserSettingsResponse(
    Guid UserId,
    string NativeLanguage,
    string StudyLanguage,
    string ExplanationLanguage,
    string CurrentLevel,
    string SelectedTutorId,
    string SpeechVoice,
    decimal SpeechSpeed,
    bool ConversationModeEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
