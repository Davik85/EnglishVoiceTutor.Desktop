namespace EnglishVoiceTutor.Api.Options;

public sealed class GooglePlayBillingOptions
{
    public const string SectionName = "GooglePlayBilling";

    public bool Enabled { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public List<string> AllowedProductIds { get; set; } = [];
}
