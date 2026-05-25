namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendTrialClaimResponse
{
    public Guid UserId { get; init; }
    public bool Claimed { get; init; }
    public bool AlreadyClaimed { get; init; }
    public bool TrialActive { get; init; }
    public DateTimeOffset? TrialEndsAtUtc { get; init; }
    public string Message { get; init; } = string.Empty;
    public BackendSubscriptionStatusResponse Status { get; init; } = new();
}
