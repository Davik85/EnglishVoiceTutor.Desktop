namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendSubscriptionStatusResponse
{
    public Guid UserId { get; init; }
    public string PlanId { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public bool PremiumActive { get; init; }
    public bool TrialActive { get; init; }
    public DateTimeOffset? TrialEndsAtUtc { get; init; }
    public string SubscriptionStatus { get; init; } = string.Empty;
    public string BillingProvider { get; init; } = string.Empty;
    public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
    public bool FreeLessonUsedToday { get; init; }
    public int FreeLessonRemainingToday { get; init; }
    public string FreeLessonConsumptionRule { get; init; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool EnforcementEnabled { get; init; }
}
