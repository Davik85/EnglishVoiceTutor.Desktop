namespace EnglishVoiceTutor.Api.Models;

public sealed class AudioSpeechRequest
{
    public string Text { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public string? Model { get; init; }

    public string? Instructions { get; init; }

    public double? SpeechSpeed { get; init; }
}
