namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonMetadata
{
    public string? Level { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Subtopic { get; set; } = string.Empty;

    public string LessonType { get; set; } = string.Empty;

    public List<string> SupportedLevels { get; set; } = [];

    /// <summary>Legacy scenario metadata field retained for JSON compatibility only. Runtime lesson length is owned by level profiles.</summary>
    public int SoftWrapUpAfterUserTurn { get; set; }

    /// <summary>Legacy scenario metadata field retained for JSON compatibility only. Runtime final-turn timing is owned by level profiles.</summary>
    public int FinalMessageAtUserTurn { get; set; }

    public bool SetupAndContextChoiceCountAsLessonTurns { get; set; }
}
