namespace EnglishVoiceTutor.Api.Options;

public sealed class WebsiteContentOptions
{
    public const string SectionName = "WebsiteContent";

    public string StorageJsonPath { get; set; } = "site/content/website-content.json";

    public string PublicSiteRoot { get; set; } = "site/public";
}
