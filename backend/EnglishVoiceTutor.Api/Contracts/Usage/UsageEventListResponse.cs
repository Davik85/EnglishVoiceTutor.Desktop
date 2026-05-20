namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class UsageEventListResponse
{
    public required IReadOnlyList<UsageEventResponse> Events { get; init; }
}
