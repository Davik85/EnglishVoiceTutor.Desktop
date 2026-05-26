using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserLookupResponse
{
    public AdminUserSnapshot User { get; set; } = new();
    public AdminUserProfileSnapshot? Profile { get; set; }
    public AdminUserSettingsSnapshot? Settings { get; set; }
    public SubscriptionStatusResponse SubscriptionStatus { get; set; } = new();
    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class AdminUserSnapshot
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
