namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendFeedbackDto
{
    public string ShortText { get; init; } = string.Empty;

    public string CorrectedVersion { get; init; } = string.Empty;

    public string GrammarTip { get; init; } = string.Empty;

    public string VocabularyTip { get; init; } = string.Empty;

    public string CultureTip { get; init; } = string.Empty;

    public string NaturalVersion { get; init; } = string.Empty;
}
