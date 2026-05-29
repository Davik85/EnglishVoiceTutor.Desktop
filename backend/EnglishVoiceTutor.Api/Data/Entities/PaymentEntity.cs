namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class PaymentEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string InternalPlanId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long? AmountMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public string? ProviderPriceId { get; set; }
    public string? ProviderProductId { get; set; }
    public string? ProviderEventId { get; set; }
    public string? ProviderEventType { get; set; }
    public DateTimeOffset? ProviderEventOccurredAtUtc { get; set; }
    public string? SafeMetadataJson { get; set; }
    public string? ProviderPayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? BilledAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
