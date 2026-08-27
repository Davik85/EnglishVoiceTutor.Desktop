namespace EnglishVoiceTutor.Api.Services.Billing;

public interface IGooglePlayTrialDeferralService
{
    Task<GooglePlayTrialDeferralResult> ProcessAsync(
        Guid userId,
        string purchaseToken,
        string protectedPurchaseToken,
        CancellationToken cancellationToken);
}

public enum GooglePlayTrialDeferralResultCode
{
    NotRequired,
    Completed,
    Pending,
    AmbiguousTerminal
}

public sealed record GooglePlayTrialDeferralResult(GooglePlayTrialDeferralResultCode Code);
