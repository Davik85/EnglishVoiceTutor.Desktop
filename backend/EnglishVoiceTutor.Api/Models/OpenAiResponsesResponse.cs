using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiResponsesResponse
{
    [JsonPropertyName("output_text")]
    public string OutputText { get; init; } = string.Empty;
}
