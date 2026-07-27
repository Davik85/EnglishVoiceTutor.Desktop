namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IProviderSubscriptionPeriodPersistenceService
{
    Task<ProviderSubscriptionPeriodPersistenceResult> ApplyAsync(
        ProviderSubscriptionPeriodPersistenceRequest request,
        CancellationToken cancellationToken);
}

public sealed record ProviderSubscriptionPeriodPersistenceRequest(
    Guid UserId,
    Guid SubscriptionId,
    string? ProviderProductId,
    DateTimeOffset PeriodStartsAtUtc,
    DateTimeOffset PeriodExpiresAtUtc,
    bool IsTestPurchase);

public enum ProviderSubscriptionPeriodPersistenceResultCode
{
    Applied,
    AlreadyCurrent,
    InvalidInput,
    SubscriptionNotFound,
    SubscriptionOwnershipConflict,
    UnsupportedSubscription,
    TestPurchaseNotSupported,
    TemporarilyUnavailable
}

public sealed record ProviderSubscriptionPeriodPersistenceResult(ProviderSubscriptionPeriodPersistenceResultCode Code);
