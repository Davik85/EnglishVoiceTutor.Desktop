namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendHealthResponse
{
    public string Status { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public DateTimeOffset CheckedAtUtc { get; init; }
}
