namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminWebsiteCmsSectionInitializationResponse
{
    public int CreatedCount { get; set; }
    public int ExistingCount { get; set; }
    public int TotalExpectedCount { get; set; }
    public IReadOnlyList<AdminWebsiteCmsSectionInitializationResult> Sections { get; set; } = [];
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminWebsiteCmsSectionInitializationResult
{
    public string SectionKey { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool Created { get; set; }
}
