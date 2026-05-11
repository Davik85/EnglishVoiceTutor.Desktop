namespace EnglishVoiceTutor.Desktop.Models;

public sealed record TutorAvatarOption(
    string Id,
    string DisplayName,
    string AgeText,
    string Location,
    string Role,
    string InterestsText,
    string PersonalityText,
    string SpeakingStyleText,
    string ShortDescription);
