namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class TutorProfile
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string HomeCity { get; set; } = string.Empty;

    public string CountryOrRegion { get; set; } = string.Empty;

    public string Studies { get; set; } = string.Empty;

    public List<string> Hobbies { get; set; } = [];

    public List<string> CommunicationStyle { get; set; } = [];

    public Dictionary<string, string> SpeakingRules { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> IdentityRules { get; set; } = [];

    public string Location => HomeCity;

    public string Background => string.IsNullOrWhiteSpace(Studies) ? string.Empty : $"studies {Studies}";

    public List<string> Interests => Hobbies;

    public List<string> BehaviorRules => IdentityRules;
}
