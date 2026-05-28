namespace EnglishVoiceTutor.Api.Options;

public sealed class PaddleBillingOptions
{
    public const string SectionName = "PaddleBilling";

    public bool CheckoutAdapterEnabled { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string PremiumPriceId { get; set; } = string.Empty;
}
