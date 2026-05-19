namespace EnglishVoiceTutor.Desktop.Models;

public sealed class StartBackendLessonSessionRequest
{
    public string LessonContentId { get; set; } = string.Empty;
    public string StudyLanguage { get; set; } = string.Empty;
    public string TopicId { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string SubtopicId { get; set; } = string.Empty;
    public string SubtopicTitle { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? SelectedContextId { get; set; }
    public string? SelectedContextTitle { get; set; }
    public string ModeUsed { get; set; } = string.Empty;
}
