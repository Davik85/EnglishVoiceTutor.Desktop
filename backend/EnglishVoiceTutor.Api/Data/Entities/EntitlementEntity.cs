namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class EntitlementEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public Guid? SubscriptionId { get; set; }
    public string EntitlementType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public SubscriptionEntity? Subscription { get; set; }
}
