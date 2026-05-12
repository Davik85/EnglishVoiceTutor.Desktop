using EnglishVoiceTutor.Desktop.Constants;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed class AudioInputDeviceOption
{
    public string Id { get; init; } = AudioConstants.DefaultAudioInputDeviceId;

    public string DisplayName { get; init; } = string.Empty;

    public int DeviceNumber { get; init; } = AudioConstants.DefaultAudioInputDeviceNumber;

    public bool IsDefault { get; init; }
}
