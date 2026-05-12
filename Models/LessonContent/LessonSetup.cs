namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonSetup
{
    public List<string> FirstBotMessageShouldExplain { get; set; } = [];

    public string SetupMessage { get; set; } = string.Empty;

    public bool SetupAndContextChoiceCountAsLessonTurns { get; set; }
}
