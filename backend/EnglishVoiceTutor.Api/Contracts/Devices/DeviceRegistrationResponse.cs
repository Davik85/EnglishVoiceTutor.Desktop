namespace EnglishVoiceTutor.Api.Contracts.Devices;

public sealed class DeviceRegistrationResponse
{
    public bool Registered { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
    public string TrackingScope { get; init; } = "authenticated_device_record";
}
