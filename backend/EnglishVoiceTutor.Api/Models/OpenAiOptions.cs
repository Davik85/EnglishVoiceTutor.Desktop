using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = OpenAiConstants.DefaultModel;
}
