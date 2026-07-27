namespace EnglishVoiceTutor.Api.Contracts.Billing;

public sealed class GooglePlayPurchaseVerificationResponse
{
    public string Result { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool SubscriptionStatusRefreshRecommended { get; init; }
}
