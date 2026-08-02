namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class GooglePlayRtdnEventEntity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PubSubMessageId { get; set; } = string.Empty;
    public string PubSubSubscription { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string NotificationKind { get; set; } = string.Empty;
    public string? PurchaseTokenFingerprint { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? SafeErrorCode { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
}
