namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class TargetLanguage
{
    public List<string> KeyPhrases { get; set; } = [];

    public List<string> UsefulConstructions { get; set; } = [];

    public List<string> GrammarFocus { get; set; } = [];

    public List<string> PronunciationFocus { get; set; } = [];
}
