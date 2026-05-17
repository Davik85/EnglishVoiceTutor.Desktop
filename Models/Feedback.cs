namespace EnglishVoiceTutor.Desktop.Models;

public sealed class Feedback
{
    public Feedback(
        string type,
        string shortText,
        string correctedVersion,
        string grammarTip,
        string vocabularyTip,
        string cultureTip,
        string naturalVersion,
        string shortTextTranslation,
        string correctedVersionTranslation,
        string grammarTipTranslation,
        string vocabularyTipTranslation,
        string cultureTipTranslation,
        string naturalVersionTranslation)
    {
        Type = type;
        ShortText = shortText;
        CorrectedVersion = correctedVersion;
        GrammarTip = grammarTip;
        VocabularyTip = vocabularyTip;
        CultureTip = cultureTip;
        NaturalVersion = naturalVersion;
        ShortTextTranslation = shortTextTranslation;
        CorrectedVersionTranslation = correctedVersionTranslation;
        GrammarTipTranslation = grammarTipTranslation;
        VocabularyTipTranslation = vocabularyTipTranslation;
        CultureTipTranslation = cultureTipTranslation;
        NaturalVersionTranslation = naturalVersionTranslation;
    }

    public string Type { get; }

    public string ShortText { get; }

    public string CorrectedVersion { get; }

    public string GrammarTip { get; }

    public string VocabularyTip { get; }

    public string CultureTip { get; }

    public string NaturalVersion { get; }

    public string ShortTextTranslation { get; set; }

    public string CorrectedVersionTranslation { get; set; }

    public string GrammarTipTranslation { get; set; }

    public string VocabularyTipTranslation { get; set; }

    public string CultureTipTranslation { get; set; }

    public string NaturalVersionTranslation { get; set; }

    public bool HasShortText => !string.IsNullOrWhiteSpace(ShortText);

    public bool HasCorrectedVersion => !string.IsNullOrWhiteSpace(CorrectedVersion);

    public bool HasGrammarTip => !string.IsNullOrWhiteSpace(GrammarTip);

    public bool HasVocabularyTip => !string.IsNullOrWhiteSpace(VocabularyTip);

    public bool HasCultureTip => !string.IsNullOrWhiteSpace(CultureTip);

    public bool HasNaturalVersion => !string.IsNullOrWhiteSpace(NaturalVersion);

    public bool HasTranslations =>
        !string.IsNullOrWhiteSpace(ShortTextTranslation)
        && !string.IsNullOrWhiteSpace(CorrectedVersionTranslation)
        && !string.IsNullOrWhiteSpace(GrammarTipTranslation)
        && !string.IsNullOrWhiteSpace(VocabularyTipTranslation)
        && !string.IsNullOrWhiteSpace(CultureTipTranslation)
        && !string.IsNullOrWhiteSpace(NaturalVersionTranslation);
}
