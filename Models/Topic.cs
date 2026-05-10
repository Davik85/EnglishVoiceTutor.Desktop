namespace EnglishVoiceTutor.Desktop.Models;

public sealed record Topic(int Id, string Title, string Description)
{
    public string DisplayTitle { get; init; } = Title;

    public string DisplayDescription { get; init; } = Description;
}
