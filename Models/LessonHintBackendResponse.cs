using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed class LessonHintBackendResponse
{
    [JsonPropertyName("hintText")]
    public string HintText { get; init; } = string.Empty;
}
