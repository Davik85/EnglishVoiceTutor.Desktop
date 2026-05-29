namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record BillingEventSubscriptionSnapshotResult(
    int CheckedCount,
    int UpsertedCount,
    int IgnoredOlderCount,
    int BlockedCount,
    int FailedCount,
    int AlreadySkippedCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
