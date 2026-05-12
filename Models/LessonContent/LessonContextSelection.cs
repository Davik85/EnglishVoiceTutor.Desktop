namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonContextSelection
{
    public bool CustomContextAllowed { get; set; }

    public string CustomContextValidationMode { get; set; } = string.Empty;

    public string ValidCustomContextDescription { get; set; } = string.Empty;

    public List<string> ValidCustomContextKeywords { get; set; } = [];

    public List<string> OffTopicExamples { get; set; } = [];

    public string InvalidContextRedirect { get; set; } = string.Empty;
}
