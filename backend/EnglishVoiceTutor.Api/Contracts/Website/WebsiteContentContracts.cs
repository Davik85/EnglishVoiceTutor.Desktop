namespace EnglishVoiceTutor.Api.Contracts.Website;

public sealed record WebsiteHomeHeaderContent(
    string LogoPath,
    string LogoAltText,
    bool ShowLogo,
    string FallbackLogoText,
    string HeaderText,
    string LanguageLine,
    string FontFamily,
    int FontSizePx,
    int FontWeight,
    string TextColor,
    string HeaderBackgroundColor,
    int PaddingBlockPx,
    int PaddingInlinePx);

public sealed record WebsiteContentDocument(WebsiteHomeHeaderContent ActiveHomeHeader, WebsiteHomeHeaderContent DraftHomeHeader);

public sealed record WebsiteHomeHeaderResponse(WebsiteHomeHeaderContent ActiveHomeHeader, WebsiteHomeHeaderContent DraftHomeHeader);

public sealed record WebsitePublishResponse(WebsiteHomeHeaderContent ActiveHomeHeader, string PublishedIndexPath, DateTimeOffset PublishedAtUtc);
