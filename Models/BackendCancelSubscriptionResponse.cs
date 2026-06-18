namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendCancelSubscriptionResponse
{
    public bool Accepted { get; init; }
    public bool Success { get; init; }
    public bool AlreadyCanceling { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string SubscriptionStatus { get; init; } = string.Empty;
    public bool CancelAtPeriodEnd { get; init; }
    public DateTimeOffset? ScheduledChangeEffectiveAtUtc { get; init; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
}
