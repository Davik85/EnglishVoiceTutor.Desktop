namespace EnglishVoiceTutor.Api.Models;

public sealed class TutorAvatarProfile
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int Age { get; init; }

    public string HomeCity { get; init; } = string.Empty;

    public string CountryOrRegion { get; init; } = string.Empty;

    public string Studies { get; init; } = string.Empty;

    public List<string> Hobbies { get; init; } = [];

    public List<string> CommunicationStyle { get; init; } = [];

    public Dictionary<string, string> SpeakingRules { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> IdentityRules { get; init; } = [];

    public string Location => HomeCity;

    public string Role => string.IsNullOrWhiteSpace(Studies) ? string.Empty : $"{Studies} student";

    public IReadOnlyList<string> Interests => Hobbies;

    public string PersonalitySummary => string.Join(", ", CommunicationStyle);

    public string SpeakingStyle => string.Join(", ", CommunicationStyle);

    public string Boundaries => string.Join(" ", IdentityRules);
}
