namespace EnglishVoiceTutor.Api.Options;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    public bool CheckoutEnabled { get; init; } = false;
    public string Provider { get; init; } = "none";
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
}
