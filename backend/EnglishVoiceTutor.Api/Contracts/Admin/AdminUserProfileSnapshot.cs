namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminUserProfileSnapshot
{
    public string? DisplayName { get; set; }
    public string? NativeLanguage { get; set; }
    public string? CurrentLevel { get; set; }
    public string? SelectedTutorId { get; set; }
    public string? Timezone { get; set; }
}
