namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class PlanEntity
{
    public Guid Id { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<SubscriptionEntity> Subscriptions { get; set; } = [];
    public ICollection<EntitlementEntity> Entitlements { get; set; } = [];
}
