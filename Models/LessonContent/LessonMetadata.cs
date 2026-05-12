namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonMetadata
{
    public string? Level { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Subtopic { get; set; } = string.Empty;

    public string LessonType { get; set; } = string.Empty;

    public List<string> SupportedLevels { get; set; } = [];

    public int SoftWrapUpAfterUserTurn { get; set; }

    public int FinalMessageAtUserTurn { get; set; }

    public bool SetupAndContextChoiceCountAsLessonTurns { get; set; }
}
