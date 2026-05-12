namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LevelProfile
{
    public string Level { get; set; } = string.Empty;

    public string DifficultyNotes { get; set; } = string.Empty;

    public string TutorLanguageStyle { get; set; } = string.Empty;

    public string ExpectedUserResponse { get; set; } = string.Empty;

    public string MinimumUserResponse { get; set; } = string.Empty;

    public string StretchUserResponse { get; set; } = string.Empty;

    public List<string> AddedKeyPhrases { get; set; } = [];

    public List<string> AddedUsefulConstructions { get; set; } = [];

    public List<string> AddedGrammarFocus { get; set; } = [];

    public string FeedbackStrictness { get; set; } = string.Empty;

    public string HintStrategy { get; set; } = string.Empty;

    public string CorrectionPriority { get; set; } = string.Empty;

    public string ConversationDepth { get; set; } = string.Empty;

    public string ExampleGoodAnswer { get; set; } = string.Empty;

    public string ExampleStretchAnswer { get; set; } = string.Empty;
}
