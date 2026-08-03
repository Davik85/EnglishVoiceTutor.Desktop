namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class GooglePlayPendingRefundReviewEntity
{
    public Guid Id { get; set; }
    public string PubSubMessageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string PendingRefundTokenFingerprint { get; set; } = string.Empty;
    public string OrderIdFingerprint { get; set; } = string.Empty;
    public string? ProtectedReviewPayload { get; set; }
    public string ProtectionFormatVersion { get; set; } = string.Empty;
    public string NotificationVersion { get; set; } = string.Empty;
    public int RefundReason { get; set; }
    public DateTimeOffset EventTimeUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset ReviewDeadlineAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LastSafeResultCode { get; set; }
    public string RefundPreference { get; set; } = string.Empty;
    public bool SampleContentProvided { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public DateTimeOffset? ProtectedPayloadDeleteAfterUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
