namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminWebsiteCmsSectionOverviewResponse
{
    public IReadOnlyList<AdminWebsiteCmsSectionOverviewItem> Sections { get; set; } = [];
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminWebsiteCmsSectionOverviewItem
{
    public string SectionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool StoredRowExists { get; set; }
    public string? ReviewStatus { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public bool DraftBodyExists { get; set; }
    public bool PublishedBodyExists { get; set; }
}
