namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminAuditActionsResponse
{
    public Guid UserId { get; init; }
    public IReadOnlyList<AdminAuditActionSnapshot> Items { get; init; } = [];
    public int Limit { get; init; }
    public DateTimeOffset CheckedAtUtc { get; init; }
}
