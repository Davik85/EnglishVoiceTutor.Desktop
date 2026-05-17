using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiAudioSpeechRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("input")]
    public string Input { get; init; } = string.Empty;

    [JsonPropertyName("voice")]
    public string Voice { get; init; } = string.Empty;

    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instructions { get; init; }

    [JsonPropertyName("speed")]
    public double Speed { get; init; } = 1.0;

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; init; } = string.Empty;
}
