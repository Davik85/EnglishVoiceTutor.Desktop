namespace EnglishVoiceTutor.Api.Models;

public sealed class BackendConfigStatusResponse
{
    public string OpenAiStatus { get; init; } = string.Empty;
    public string OpenAiModel { get; init; } = string.Empty;
}
