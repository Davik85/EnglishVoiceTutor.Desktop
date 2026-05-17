namespace EnglishVoiceTutor.Desktop.Models;

public sealed record Subtopic(int Id, int TopicId, string Title, string Description)
{
    public string TopicThemeKey => TopicId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string DisplayTitle { get; init; } = Title;

    public string DisplayDescription { get; init; } = Description;
}
