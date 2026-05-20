namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class UsageEventListResponse
{
    public required IReadOnlyList<UsageEventResponse> Items { get; init; }
}
