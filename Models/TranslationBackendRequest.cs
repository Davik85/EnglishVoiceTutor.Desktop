namespace EnglishVoiceTutor.Desktop.Models;

public sealed class TranslationBackendRequest
{
    public string Text { get; init; } = string.Empty;

    public string TargetLanguage { get; init; } = string.Empty;
}
