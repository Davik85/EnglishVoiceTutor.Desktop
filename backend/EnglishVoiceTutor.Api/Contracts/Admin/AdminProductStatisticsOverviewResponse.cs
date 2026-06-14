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
    public required IReadOnlyDictionary<string, string> Definitions { get; init; }
}
