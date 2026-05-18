namespace EnglishVoiceTutor.Api.Data.Entities;

public sealed class UserProfileEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string NativeLanguage { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string? SelectedTutorId { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
