namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class RestoreCredentialCeremonyEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string CeremonyType { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public uint ConcurrencyRevision { get; set; }
    public UserEntity? User { get; set; }
}
