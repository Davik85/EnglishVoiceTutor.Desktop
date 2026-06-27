namespace EnglishVoiceTutor.Api.Contracts.Website;

public sealed record WebsiteDesignContent(
    string HeaderBackgroundColor,
    string FooterBackgroundColor,
    string MainTextColor,
    string HeaderTextColor,
    string MainFontFamily,
    int BaseFontSizePx,
    int HeaderFontWeight,
    int ButtonBorderRadiusPx,
    string CardTextStyle);

public sealed record WebsiteContentSet(
    Dictionary<string, Dictionary<string, string>> Pages,
    WebsiteDesignContent Design);

public sealed record WebsiteContentDocument(WebsiteContentSet Active, WebsiteContentSet Draft);

public sealed record WebsiteContentResponse(WebsiteContentSet Active, WebsiteContentSet Draft);

public sealed record WebsitePreviewRequest(WebsiteContentSet Content, string PageKey);

public sealed record WebsitePreviewResponse(string PageKey, string Html, DateTimeOffset PreviewedAtUtc);

public sealed record WebsitePublishResponse(WebsiteContentSet Active, string PublicSiteRoot, IReadOnlyList<string> PublishedFiles, DateTimeOffset PublishedAtUtc);
