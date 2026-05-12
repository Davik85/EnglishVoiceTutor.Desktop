namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class HintRules
{
    public string HintStyle { get; set; } = string.Empty;

    public List<string> HintLevels { get; set; } = [];

    public string ExampleHint { get; set; } = string.Empty;
}
