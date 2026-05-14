namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class ReciprocalQuestionHandling
{
    public string IfUserAsksTutorName { get; set; } = string.Empty;

    public string IfUserAsksSimplePersonalQuestion { get; set; } = string.Empty;

    public bool MustNotIgnoreUserQuestion { get; set; }

    public bool MustNotRefuseScenarioCompatibleQuestions { get; set; }
}
