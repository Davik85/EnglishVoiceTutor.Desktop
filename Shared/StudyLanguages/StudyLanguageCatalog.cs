namespace EnglishVoiceTutor.Shared.StudyLanguages;

public static class StudyLanguageCatalog
{
    public const string DefaultStudyLanguageId = "en";

    public static readonly StudyLanguageDefinition English = new(
        Id: "en",
        EnglishName: "English",
        NativeName: "English",
        Bcp47Code: "en",
        TranscriptionLanguageCode: "en",
        TutorInstructionName: "English",
        LanguageLockName: "English",
        IsDefault: true);

    public static readonly IReadOnlyList<StudyLanguageDefinition> All =
    [
        English,
        new(
            Id: "fr",
            EnglishName: "French",
            NativeName: "Français",
            Bcp47Code: "fr",
            TranscriptionLanguageCode: "fr",
            TutorInstructionName: "French",
            LanguageLockName: "French",
            IsDefault: false),
        new(
            Id: "de",
            EnglishName: "German",
            NativeName: "Deutsch",
            Bcp47Code: "de",
            TranscriptionLanguageCode: "de",
            TutorInstructionName: "German",
            LanguageLockName: "German",
            IsDefault: false),
        new(
            Id: "pt",
            EnglishName: "Portuguese",
            NativeName: "Português",
            Bcp47Code: "pt",
            TranscriptionLanguageCode: "pt",
            TutorInstructionName: "Portuguese",
            LanguageLockName: "Portuguese",
            IsDefault: false),
        new(
            Id: "es",
            EnglishName: "Spanish",
            NativeName: "Español",
            Bcp47Code: "es",
            TranscriptionLanguageCode: "es",
            TutorInstructionName: "Spanish",
            LanguageLockName: "Spanish",
            IsDefault: false),
        new(
            Id: "it",
            EnglishName: "Italian",
            NativeName: "Italiano",
            Bcp47Code: "it",
            TranscriptionLanguageCode: "it",
            TutorInstructionName: "Italian",
            LanguageLockName: "Italian",
            IsDefault: false)
    ];

    public static StudyLanguageDefinition GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return English;
        }

        return All.FirstOrDefault(language => string.Equals(language.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? English;
    }
}
