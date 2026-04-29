namespace EnglishVoiceTutor.Desktop.Models;

public sealed record Feedback(
    string Type,
    string ShortText,
    string CorrectedVersion,
    string GrammarTip,
    string VocabularyTip,
    string CultureTip,
    string NaturalVersion,
    string ShortTextTranslation,
    string CorrectedVersionTranslation,
    string GrammarTipTranslation,
    string VocabularyTipTranslation,
    string CultureTipTranslation,
    string NaturalVersionTranslation);
