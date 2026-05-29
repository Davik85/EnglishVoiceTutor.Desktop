namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record BillingEventSubscriptionSnapshotResult(
    int CheckedCount,
    int UpsertedCount,
    int IgnoredOlderCount,
    int BlockedCount,
    int FailedCount,
    int AlreadySkippedCount,
    int ProviderEventEntitlementExpiredCount,
    DateTimeOffset? ProviderEventEntitlementExpiresAtUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
