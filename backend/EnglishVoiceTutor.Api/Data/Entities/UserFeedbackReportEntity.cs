namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class UserFeedbackReportEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReportedAiText { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClientPlatform { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public UserEntity User { get; set; } = null!;
    public ICollection<UserFeedbackReportReplyEntity> Replies { get; set; } = [];
}
