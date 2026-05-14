namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class ConversationFlow
{
    public string Opening { get; set; } = string.Empty;

    public string DefaultOpeningExample { get; set; } = string.Empty;

    public string FirstUserTask { get; set; } = string.Empty;

    public string ExpectedPattern { get; set; } = string.Empty;

    public List<string> GuidedPracticeFollowUpQuestions { get; set; } = [];

    public string VariationOrComplication { get; set; } = string.Empty;

    public string ExampleTutorLine { get; set; } = string.Empty;

    public string CorrectionMoment { get; set; } = string.Empty;

    public string CorrectionStyle { get; set; } = string.Empty;

    public int WrapUpAfterUserTurn { get; set; }

    public string WrapUpMessage { get; set; } = string.Empty;

    public int FinalMessageAtUserTurn { get; set; }

    public string FinalMessage { get; set; } = string.Empty;

    public string WrapUpIntent { get; set; } = string.Empty;

    public string FinalMessageIntent { get; set; } = string.Empty;
}
