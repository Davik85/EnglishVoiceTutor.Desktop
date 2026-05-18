namespace EnglishVoiceTutor.Desktop.Models;

public sealed class DatabaseHealthResponse
{
    public string Status { get; init; } = string.Empty;

    public bool CanConnect { get; init; }

    public string Provider { get; init; } = string.Empty;

    public DateTimeOffset CheckedAtUtc { get; init; }

    public string? Error { get; init; }
}
