namespace EnglishVoiceTutor.Api.Contracts.Devices;

public sealed class DeviceRegistrationRequest
{
    public string Platform { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string AppVersion { get; init; } = string.Empty;
}
