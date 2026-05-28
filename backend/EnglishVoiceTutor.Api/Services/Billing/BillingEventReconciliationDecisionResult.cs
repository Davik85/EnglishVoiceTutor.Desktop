namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record BillingEventReconciliationDecisionResult(
    int CheckedCount,
    int MarkedPendingCount,
    int IgnoredCount,
    int BlockedCount,
    int FailedCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
