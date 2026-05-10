namespace EnglishVoiceTutor.Api.Models;

public sealed record TutorAvatarProfile(
    string Id,
    string DisplayName,
    int Age,
    string Location,
    string Role,
    IReadOnlyList<string> Interests,
    string PersonalitySummary,
    string SpeakingStyle,
    string Boundaries);
