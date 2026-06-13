namespace EnglishVoiceTutor.Desktop.Models;

public sealed class RuntimeTutorOption
{
    public string TutorId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
