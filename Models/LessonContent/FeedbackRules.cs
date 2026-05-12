namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class FeedbackRules
{
    public Dictionary<string, string> LevelRules { get; set; } = [];

    public string FeedbackLength { get; set; } = string.Empty;

    public string FeedbackStyle { get; set; } = string.Empty;
}
