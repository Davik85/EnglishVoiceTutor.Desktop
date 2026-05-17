namespace EnglishVoiceTutor.Api.Models;

public sealed class TranslationRequest
{
    public string Text { get; init; } = string.Empty;

    public string TargetLanguage { get; init; } = string.Empty;

    public string SourceLanguageId { get; init; } = string.Empty;

    public string SourceLanguageName { get; init; } = string.Empty;

    public string SourceLanguageNativeName { get; init; } = string.Empty;

    public string SourceLanguageCode { get; init; } = string.Empty;
}
