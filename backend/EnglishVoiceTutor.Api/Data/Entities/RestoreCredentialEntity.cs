namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class RestoreCredentialEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public byte[] CredentialId { get; set; } = [];
    public byte[] UserHandle { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public uint SignatureCounter { get; set; }
    public string CredentialKind { get; set; } = "restore";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastUsedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public UserEntity User { get; set; } = null!;
}
