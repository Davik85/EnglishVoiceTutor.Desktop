namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class LessonSetupLocalization
{
    public string SetupMessageTemplate { get; set; } = string.Empty;

    public Dictionary<string, string> ContextVariantTitles { get; set; } = new(StringComparer.Ordinal);
}

public sealed class LocalizedLessonSetup
{
    public string ResolvedStudyLanguageId { get; set; } = string.Empty;

    public string? SetupMessageTemplate { get; set; }

    public Dictionary<string, string> ContextVariantDisplayTitles { get; set; } = new(StringComparer.Ordinal);

    public string Source { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool FallbackUsed { get; set; }
}
