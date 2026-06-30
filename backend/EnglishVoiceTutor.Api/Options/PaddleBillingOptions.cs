namespace EnglishVoiceTutor.Api.Options;

public sealed class PaddleBillingOptions
{
    public const string SectionName = "PaddleBilling";

    public bool CheckoutAdapterEnabled { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string PremiumPriceId { get; set; } = string.Empty;

    public string PremiumLivePriceId { get; set; } = string.Empty;

    public string PremiumProductId { get; set; } = string.Empty;

    public string PremiumLiveProductId { get; set; } = string.Empty;

    public string ExpectedCustomDataApp { get; set; } = "language_voice_tutor";

    public string ExpectedCustomDataProduct { get; set; } = "language_voice_tutor_pro";

    public string CheckoutUrl { get; set; } = "https://languagevoicetutor.com/pay.html";

    public string ClientSideToken { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "1";

    public string SandboxBaseUrl { get; set; } = "https://sandbox-api.paddle.com";

    public string LiveBaseUrl { get; set; } = "https://api.paddle.com";

    public string CheckoutCreatedMessage { get; set; } = "Checkout session created.";
}
