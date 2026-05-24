namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class TrialGrantEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string SourcePlatform { get; set; } = string.Empty;
    public string? DeviceFingerprintHash { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
