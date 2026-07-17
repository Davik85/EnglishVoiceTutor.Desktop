namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class UserFeedbackReportReplyEntity
{
    public Guid Id { get; set; }
    public Guid FeedbackReportId { get; set; }
    public Guid AdminUserId { get; set; }
    public string ReplyText { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }

    public UserFeedbackReportEntity FeedbackReport { get; set; } = null!;
    public AdminUserEntity AdminUser { get; set; } = null!;
}
