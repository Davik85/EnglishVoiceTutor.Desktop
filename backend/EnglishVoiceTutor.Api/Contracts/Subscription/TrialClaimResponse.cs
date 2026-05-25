namespace EnglishVoiceTutor.Api.Contracts.Subscription;

public sealed class TrialClaimResponse
{
    public Guid UserId { get; set; }
    public bool Claimed { get; set; }
    public bool AlreadyClaimed { get; set; }
    public bool TrialActive { get; set; }
    public DateTimeOffset? TrialEndsAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
    public SubscriptionStatusResponse Status { get; set; } = new();
}
