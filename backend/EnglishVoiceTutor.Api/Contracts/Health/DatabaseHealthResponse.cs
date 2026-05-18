namespace EnglishVoiceTutor.Api.Contracts.Health;

public sealed record DatabaseHealthResponse(
    string Status,
    bool CanConnect,
    string Provider,
    DateTimeOffset CheckedAtUtc,
    string? Error);
