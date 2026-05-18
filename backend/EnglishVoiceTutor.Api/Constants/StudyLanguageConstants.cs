namespace EnglishVoiceTutor.Api.Constants;

public static class StudyLanguageConstants
{
    public const string English = "English";
    public const string French = "French";
    public const string German = "German";
    public const string Portuguese = "Portuguese";
    public const string Spanish = "Spanish";
    public const string Italian = "Italian";

    public const string DefaultStudyLanguage = English;

    public static readonly string[] SupportedStudyLanguages =
    [
        English,
        French,
        German,
        Portuguese,
        Spanish,
        Italian
    ];

    public static bool IsSupported(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && SupportedStudyLanguages.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string ToCanonicalValue(string value)
    {
        return SupportedStudyLanguages.First(language => string.Equals(language, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
