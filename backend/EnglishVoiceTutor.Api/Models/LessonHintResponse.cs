using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class LessonHintResponse
{
    [JsonPropertyName("hintText")]
    public string HintText { get; init; } = string.Empty;
}
