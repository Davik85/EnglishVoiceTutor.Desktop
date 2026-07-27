namespace EnglishVoiceTutor.Api.Contracts.Billing;

public sealed class GooglePlayPurchaseVerificationRequest
{
    public string PurchaseToken { get; init; } = string.Empty;
}
