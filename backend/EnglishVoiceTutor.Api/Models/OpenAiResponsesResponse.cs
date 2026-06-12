using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiResponsesResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("output_text")]
    public string OutputText { get; init; } = string.Empty;

    [JsonPropertyName("output")]
    public IReadOnlyList<OpenAiOutputItem> Output { get; init; } = [];

    [JsonPropertyName("usage")]
    public OpenAiResponseUsage? Usage { get; init; }
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

public sealed class OpenAiResponseUsage
{
    [JsonPropertyName("input_tokens")]
    public long? InputTokens { get; init; }

    [JsonPropertyName("output_tokens")]
    public long? OutputTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public long? TotalTokens { get; init; }

    [JsonPropertyName("input_tokens_details")]
    public OpenAiResponseTokenDetails? InputTokensDetails { get; init; }

    [JsonPropertyName("output_tokens_details")]
    public OpenAiResponseTokenDetails? OutputTokensDetails { get; init; }
}

public sealed class OpenAiResponseTokenDetails
{
    [JsonPropertyName("cached_tokens")]
    public long? CachedTokens { get; init; }

    [JsonPropertyName("audio_tokens")]
    public long? AudioTokens { get; init; }
}
