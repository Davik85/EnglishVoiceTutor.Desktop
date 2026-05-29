namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed record BillingEventPaymentPersistenceResult(
    int CheckedCount,
    int PersistedOrUpdatedCount,
    int AlreadyCurrentCount,
    int BlockedCount,
    int FailedCount,
    string? PaymentStatus,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
