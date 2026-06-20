namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminBillingCancelRenewalResponse
{
    public Guid UserId { get; init; }
    public string ResultCode { get; init; } = string.Empty;
    public bool Accepted { get; init; }
    public bool Success { get; init; }
    public bool AlreadyCanceling { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string SubscriptionStatus { get; init; } = string.Empty;
    public bool CancelAtPeriodEnd { get; init; }
    public string? ScheduledChangeAction { get; init; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; init; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
    public bool AuditWritten { get; init; }
}
