namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class SubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProviderSubscriptionId { get; set; }
    public string? ProviderCustomerId { get; set; }
    public string? ProviderPriceId { get; set; }
    public string? ProviderProductId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? CurrentPeriodStartUtc { get; set; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? ScheduledChangeAction { get; set; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; set; }
    public string? LastProviderEventId { get; set; }
    public string? LastProviderEventType { get; set; }
    public DateTimeOffset? LastProviderEventOccurredAtUtc { get; set; }
    public DateTimeOffset? LastSyncedAtUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public ICollection<EntitlementEntity> Entitlements { get; set; } = [];
}
