namespace EnglishVoiceTutor.Api.Models;

public sealed class AudioSpeechRequest
{
    public string Text { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public double? SpeechSpeed { get; init; }
}
