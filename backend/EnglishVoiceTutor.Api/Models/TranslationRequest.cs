namespace EnglishVoiceTutor.Api.Models;

public sealed class TranslationRequest
{
    public string Text { get; init; } = string.Empty;

    public string TargetLanguage { get; init; } = string.Empty;
}
