namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class PaddleWebhookEventEntity
{
    public Guid Id { get; set; }
    public string PaddleEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset? OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public string? PaddleNotificationId { get; set; }
    public string? PaddleTransactionId { get; set; }
    public string? PaddleSubscriptionId { get; set; }
    public string? PaddleCustomerId { get; set; }
    public Guid? InternalUserId { get; set; }
    public string? InternalPlanId { get; set; }
    public string RawPayload { get; set; } = string.Empty;
    public string? SignatureHeader { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
