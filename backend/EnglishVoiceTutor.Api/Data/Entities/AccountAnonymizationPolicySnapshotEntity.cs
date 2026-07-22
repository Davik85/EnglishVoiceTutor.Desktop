namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class AccountAnonymizationPolicySnapshotEntity
{
    public Guid Id { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public string VersionHash { get; set; } = string.Empty;
    public string CategoryDecisionsJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
