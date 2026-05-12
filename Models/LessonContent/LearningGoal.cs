namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LearningGoal
{
    public string Goal { get; set; } = string.Empty;

    public List<string> CanDoStatements { get; set; } = [];
}
