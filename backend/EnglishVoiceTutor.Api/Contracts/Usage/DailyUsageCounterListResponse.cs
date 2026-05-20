namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class DailyUsageCounterListResponse
{
    public required IReadOnlyList<DailyUsageCounterResponse> Items { get; init; }
}
