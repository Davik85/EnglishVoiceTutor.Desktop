using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiResponsesResponse
{
    [JsonPropertyName("output")]
    public IReadOnlyList<OpenAiOutputItem> Output { get; init; } = [];
}

public sealed class OpenAiOutputItem
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public IReadOnlyList<OpenAiContentItem> Content { get; init; } = [];
}

public sealed class OpenAiContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}
