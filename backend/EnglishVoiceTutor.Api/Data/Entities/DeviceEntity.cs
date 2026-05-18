namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class DeviceEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
