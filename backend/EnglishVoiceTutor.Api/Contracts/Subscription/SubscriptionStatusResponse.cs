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
    public string CurrentAccessTier { get; set; } = "free";
    public string CurrentAccessSource { get; set; } = "free";
    public bool CurrentAccessActive { get; set; }
    public DateTimeOffset? CurrentAccessStartsAtUtc { get; set; }
    public DateTimeOffset? CurrentAccessEndsAtUtc { get; set; }
    public string CurrentAccessDisplayCode { get; set; } = "current_access_free";
    public bool DailyFreeLimitApplies { get; set; } = true;
    public string DailyFreeLessonsLabelCode { get; set; } = "daily_free_lessons_remaining";
    public DateTimeOffset? ScheduledPaidPremiumStartUtc { get; set; }
    public DateTimeOffset? ScheduledPaidPremiumEndUtc { get; set; }
    public bool HasScheduledPaidPremium { get; set; }
    public string ScheduledPaidPremiumSource { get; set; } = string.Empty;
    public string ScheduledPaidPremiumLabelCode { get; set; } = string.Empty;
    public string CurrentTariffId { get; set; } = "free";
    public string CurrentTariffName { get; set; } = "Free";
    public string CurrentTariffDisplayCode { get; set; } = "free";
    public string FreeLessonsRemainingDisplayCode { get; set; } = "numeric";
    public int? FreeLessonsRemainingToday { get; set; }
    public string PremiumDisplayStatusCode { get; set; } = "inactive";
    public DateTimeOffset? PremiumStartsAtUtc { get; set; }
    public DateTimeOffset? PremiumEndsAtUtc { get; set; }
    public string AutoRenewalStatusCode { get; set; } = "inactive";
    public DateTimeOffset LearnerSubscriptionSummaryUpdatedAtUtc { get; set; }
}
