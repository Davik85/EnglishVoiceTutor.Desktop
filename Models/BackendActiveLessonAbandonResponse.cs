using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendActiveLessonAbandonResponse
{
    [JsonPropertyName("released")]
    public bool Released { get; init; }

    [JsonPropertyName("sessionId")]
    public Guid? SessionId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
