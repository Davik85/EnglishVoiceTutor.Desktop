namespace EnglishVoiceTutor.Api.Contracts.Health;

public sealed record HealthResponse(
    string Status,
    string Environment,
    DateTimeOffset CheckedAtUtc);
