namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminProductStatisticsOverviewResponse
{
    public DateTimeOffset CheckedAtUtc { get; init; }
    public int WindowDays { get; init; }
    public DateTimeOffset WindowStartUtc { get; init; }
    public int TotalInstallations { get; init; }
    public int RegisteredUsersTotal { get; init; }
    public int ActiveTrialsNow { get; init; }
    public int ActiveUsersLast30Days { get; init; }
    public int ActivePremiumUsersNow { get; init; }
    public int ActiveFreeUsersLast30Days { get; init; }
    public required IReadOnlyList<AdminLanguageDistributionItem> StudyLanguageDistribution { get; init; }
    public required IReadOnlyList<AdminLanguageDistributionItem> SelectedStudyLanguageDistribution { get; init; }
    public required IReadOnlyList<AdminLanguageDistributionItem> PracticedStudyLanguageDistributionLast30Days { get; init; }
    public required IReadOnlyList<AdminLanguageDistributionItem> NativeLanguageDistribution { get; init; }
    public required IReadOnlyDictionary<string, string> Definitions { get; init; }
}

public sealed class AdminLanguageDistributionItem
{
    public required string Language { get; init; }
    public int UserCount { get; init; }
    public decimal Percentage { get; init; }
}
