namespace EnglishVoiceTutor.Desktop.Models;

public class LessonHistoryItem
{
    public Guid Id { get; set; }

    public DateTime CompletedAt { get; set; }

    public string SelectedLevel { get; set; } = string.Empty;

    public Guid? OwnerUserId { get; set; }

    public string? OwnerEmail { get; set; }

    public string? OwnerKey { get; set; }

    public string TopicTitle { get; set; } = string.Empty;

    public string SubtopicTitle { get; set; } = string.Empty;

    public string GoodText { get; set; } = string.Empty;

    public string ImproveText { get; set; } = string.Empty;

    public IReadOnlyList<string> UsefulPhrases { get; set; } = [];
}
