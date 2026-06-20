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
    public bool CancelAtPeriodEnd { get; init; }
    public string? ScheduledChangeAction { get; init; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; init; }
    public bool HasActivePaidProviderSubscription { get; init; }
    public bool? CanRequestCancelRenewal { get; init; }
    public string RenewalStatus { get; init; } = string.Empty;
    public string NextRenewalState { get; init; } = string.Empty;
    public string CancellationExplanationCode { get; init; } = string.Empty;
    public DateTimeOffset? PaidAccessUntilUtc { get; init; }
    public bool HasFuturePremiumEntitlement { get; init; }
    public DateTimeOffset? FuturePremiumStartsAtUtc { get; init; }
    public DateTimeOffset? FuturePremiumExpiresAtUtc { get; init; }
    public bool FreeLessonUsedToday { get; init; }
    public int FreeLessonRemainingToday { get; init; }
    public string FreeLessonConsumptionRule { get; init; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool EnforcementEnabled { get; init; }
    public string CurrentAccessTier { get; init; } = string.Empty;
    public string CurrentAccessSource { get; init; } = string.Empty;
    public bool CurrentAccessActive { get; init; }
    public DateTimeOffset? CurrentAccessStartsAtUtc { get; init; }
    public DateTimeOffset? CurrentAccessEndsAtUtc { get; init; }
    public string CurrentAccessDisplayCode { get; init; } = string.Empty;
    public bool? DailyFreeLimitApplies { get; init; }
    public string DailyFreeLessonsLabelCode { get; init; } = string.Empty;
    public DateTimeOffset? ScheduledPaidPremiumStartUtc { get; init; }
    public DateTimeOffset? ScheduledPaidPremiumEndUtc { get; init; }
    public bool HasScheduledPaidPremium { get; init; }
    public string ScheduledPaidPremiumSource { get; init; } = string.Empty;
    public string ScheduledPaidPremiumLabelCode { get; init; } = string.Empty;
}
