namespace EnglishVoiceTutor.Desktop.Models;

public sealed class AudioSpeechBackendRequest
{
    public string Text { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public double? SpeechSpeed { get; init; }
}
