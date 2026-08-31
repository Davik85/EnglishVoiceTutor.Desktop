namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public UserProfileEntity? Profile { get; set; }
    public UserSettingsEntity? Settings { get; set; }
    public ICollection<LessonSessionEntity> LessonSessions { get; set; } = [];
    public ICollection<UsageEventEntity> UsageEvents { get; set; } = [];
    public ICollection<DailyUsageCounterEntity> DailyUsageCounters { get; set; } = [];
    public ICollection<SubscriptionEntity> Subscriptions { get; set; } = [];
    public ICollection<PaymentEntity> Payments { get; set; } = [];
    public ICollection<DeviceEntity> Devices { get; set; } = [];
    public ICollection<EntitlementEntity> Entitlements { get; set; } = [];
    public ICollection<TrialGrantEntity> TrialGrants { get; set; } = [];
    public ICollection<DailyFreeLessonUsageEntity> DailyFreeLessonUsages { get; set; } = [];
    public ICollection<AdminActionEntity> AdminActionsCreated { get; set; } = [];
    public ICollection<AdminActionEntity> AdminActionsReceived { get; set; } = [];
    public ICollection<PasswordResetTokenEntity> PasswordResetTokens { get; set; } = [];
    public ICollection<UserRefreshTokenEntity> RefreshTokens { get; set; } = [];
    public ICollection<RestoreCredentialEntity> RestoreCredentials { get; set; } = [];
    public ICollection<RestoreCredentialCeremonyEntity> RestoreCredentialCeremonies { get; set; } = [];
}
