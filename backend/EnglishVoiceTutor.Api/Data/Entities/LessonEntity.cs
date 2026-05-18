namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class LessonEntity
{
    public Guid Id { get; set; }
    public string LessonContentId { get; set; } = string.Empty;
    public string TopicId { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string SubtopicId { get; set; } = string.Empty;
    public string SubtopicTitle { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int ContentVersion { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
