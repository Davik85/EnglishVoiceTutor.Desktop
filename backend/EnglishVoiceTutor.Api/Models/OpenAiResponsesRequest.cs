using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiResponsesRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("instructions")]
    public string Instructions { get; init; } = string.Empty;

    [JsonPropertyName("input")]
    public string Input { get; init; } = string.Empty;

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; init; }

    [JsonPropertyName("text")]
    public OpenAiTextOptions? Text { get; init; }
}

public sealed class OpenAiTextOptions
{
    [JsonPropertyName("format")]
    public OpenAiTextFormat? Format { get; init; }
}

public sealed class OpenAiTextFormat
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("strict")]
    public bool Strict { get; init; }

    [JsonPropertyName("schema")]
    public JsonElement Schema { get; init; }
}
