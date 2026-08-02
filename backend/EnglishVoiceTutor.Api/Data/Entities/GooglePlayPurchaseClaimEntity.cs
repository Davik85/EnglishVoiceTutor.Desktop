namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class GooglePlayPurchaseClaimEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PurchaseTokenFingerprint { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public GooglePlayPurchaseTokenSecretEntity? PurchaseTokenSecret { get; set; }
}
