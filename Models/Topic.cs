namespace EnglishVoiceTutor.Desktop.Models;

public sealed record Topic(int Id, string Title, string Description)
{
    public string ThemeKey => Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string DisplayTitle { get; init; } = Title;

    public string DisplayDescription { get; init; } = Description;
}
