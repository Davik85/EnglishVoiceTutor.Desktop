namespace EnglishVoiceTutor.Shared.StudyLanguages;

public sealed record StudyLanguageDefinition(
    string Id,
    string EnglishName,
    string NativeName,
    string Bcp47Code,
    string TranscriptionLanguageCode,
    string TutorInstructionName,
    string LanguageLockName,
    bool IsDefault)
{
    public string DisplayName => string.Equals(EnglishName, NativeName, StringComparison.Ordinal)
        ? EnglishName
        : $"{EnglishName} / {NativeName}";
}
