namespace EnglishVoiceTutor.Shared.NativeLanguages;

public sealed record NativeLanguageDefinition(
    string Id,
    string EnglishName,
    string DisplayName,
    string Tier,
    bool IsRightToLeft = false);
