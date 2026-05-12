namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class TutorProfile
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Background { get; set; } = string.Empty;

    public List<string> Interests { get; set; } = [];

    public List<string> CommunicationStyle { get; set; } = [];

    public List<string> BehaviorRules { get; set; } = [];
}
