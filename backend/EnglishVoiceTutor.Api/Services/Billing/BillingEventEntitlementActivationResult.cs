namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record BillingEventEntitlementActivationResult(
    int CheckedCount,
    int ActivatedCount,
    int BlockedCount,
    int FailedCount,
    int AlreadySkippedCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    DateTimeOffset? EntitlementExpiresAtUtc = null);
