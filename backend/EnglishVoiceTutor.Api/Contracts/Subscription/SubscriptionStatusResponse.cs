using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Contracts.Subscription;

public sealed class SubscriptionStatusResponse
{
    public Guid UserId { get; set; }
    public string PlanId { get; set; } = SubscriptionConstants.Plans.FreePlanId;
    public string PlanName { get; set; } = SubscriptionConstants.Plans.FreePlanName;
    public bool PremiumActive { get; set; }
    public DateTimeOffset? PremiumEntitlementExpiresAtUtc { get; set; }
    public bool TrialActive { get; set; }
    public DateTimeOffset? TrialEndsAtUtc { get; set; }
    public string SubscriptionStatus { get; set; } = SubscriptionConstants.SubscriptionStatuses.None;
    public string BillingProvider { get; set; } = SubscriptionConstants.BillingProviders.None;
    public DateTimeOffset? CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? ScheduledChangeAction { get; set; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; set; }
    public bool HasActivePaidProviderSubscription { get; set; }
    public bool CanRequestCancelRenewal { get; set; }
    public string RenewalStatus { get; set; } = SubscriptionConstants.RenewalStatuses.Unknown;
    public string NextRenewalState { get; set; } = SubscriptionConstants.NextRenewalStates.Unknown;
    public string CancellationExplanationCode { get; set; } = SubscriptionConstants.CancellationExplanationCodes.Unknown;
    public DateTimeOffset? PaidAccessUntilUtc { get; set; }
    public bool ProviderSubscriptionPresent { get; set; }
    public string? LastProviderEventId { get; set; }
    public string? LastProviderEventType { get; set; }
    public DateTimeOffset? LastProviderEventOccurredAtUtc { get; set; }
    public bool HasFuturePremiumEntitlement { get; set; }
    public DateTimeOffset? FuturePremiumStartsAtUtc { get; set; }
    public DateTimeOffset? FuturePremiumExpiresAtUtc { get; set; }
    public bool FreeLessonUsedToday { get; set; }
    public int FreeLessonRemainingToday { get; set; } = SubscriptionConstants.FreeLessonsPerDay;
    public string FreeLessonConsumptionRule { get; set; } = SubscriptionConstants.FreeLessonConsumptionRule;
    public DateTimeOffset CheckedAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool EnforcementEnabled { get; set; } = SubscriptionConstants.EnforcementEnabled;
}
