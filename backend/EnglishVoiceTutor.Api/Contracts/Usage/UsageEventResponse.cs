namespace EnglishVoiceTutor.Api.Contracts.Usage;

public sealed class UsageEventResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? SessionId { get; init; }
    public string Operation { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? StudyLanguage { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal EstimatedCost { get; init; }
    public long? InputTokens { get; init; }
    public long? OutputTokens { get; init; }
    public long? InputCharacters { get; init; }
    public long? OutputBytes { get; init; }
    public long? InputAudioTokens { get; init; }
    public long? OutputAudioTokens { get; init; }
    public decimal? EstimatedDurationSeconds { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
