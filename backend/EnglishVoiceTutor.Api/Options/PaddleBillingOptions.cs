namespace EnglishVoiceTutor.Api.Options;

public sealed class PaddleBillingOptions
{
    public const string SectionName = "PaddleBilling";

    public bool CheckoutAdapterEnabled { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string PremiumPriceId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "1";

    public string SandboxBaseUrl { get; set; } = "https://sandbox-api.paddle.com";

    public string LiveBaseUrl { get; set; } = "https://api.paddle.com";

    public string HostedCheckoutUrl { get; set; } = string.Empty;

    public string CheckoutCreatedMessage { get; set; } = "Checkout session created.";
}
