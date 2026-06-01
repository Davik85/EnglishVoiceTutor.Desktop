using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendActiveLessonExistsResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("canEndOtherLesson")]
    public bool CanEndOtherLesson { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
