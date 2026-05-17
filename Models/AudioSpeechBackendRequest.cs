namespace EnglishVoiceTutor.Desktop.Models;

public sealed class AudioSpeechBackendRequest
{
    public string Text { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public string? Model { get; init; }

    public string? Instructions { get; init; }

    public double? SpeechSpeed { get; init; }

    public string TargetLanguageId { get; init; } = string.Empty;

    public string TargetLanguageName { get; init; } = string.Empty;

    public string TargetLanguageNativeName { get; init; } = string.Empty;

    public string TargetLanguageCode { get; init; } = string.Empty;
}
