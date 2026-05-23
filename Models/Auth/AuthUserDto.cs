namespace EnglishVoiceTutor.Desktop.Models.Auth;

public sealed class AuthUserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
