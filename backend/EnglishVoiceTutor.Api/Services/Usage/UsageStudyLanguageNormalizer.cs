using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class UsageStudyLanguageNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> AliasToCanonicalMap =
        BuildAliasToCanonicalMap();

    private static readonly IReadOnlyDictionary<string, string[]> CanonicalToAliasesMap =
        BuildCanonicalToAliasesMap();

    public string NormalizeOrUnknown(string? value)
    {
        return NormalizeOrDefault(value, UsageConstants.UnknownStudyLanguage);
    }

    public string NormalizeOrDefault(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();
        return AliasToCanonicalMap.TryGetValue(trimmed, out var canonical)
            ? canonical
            : trimmed;
    }

    public IReadOnlyList<string> GetAliasesForCanonical(string canonicalStudyLanguage)
    {
        if (CanonicalToAliasesMap.TryGetValue(canonicalStudyLanguage, out var aliases))
        {
            return aliases;
        }

        return [canonicalStudyLanguage];
    }

    private static IReadOnlyDictionary<string, string> BuildAliasToCanonicalMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddLanguageAliases(map, StudyLanguageConstants.English, [StudyLanguageConstants.English, "en", "en-US", "en-GB"]);
        AddLanguageAliases(map, StudyLanguageConstants.French, [StudyLanguageConstants.French, "fr", "fr-FR"]);
        AddLanguageAliases(map, StudyLanguageConstants.German, [StudyLanguageConstants.German, "de", "de-DE"]);
        AddLanguageAliases(map, StudyLanguageConstants.Portuguese, [StudyLanguageConstants.Portuguese, "pt", "pt-PT", "pt-BR"]);
        AddLanguageAliases(map, StudyLanguageConstants.Spanish, [StudyLanguageConstants.Spanish, "es", "es-ES", "es-MX"]);
        AddLanguageAliases(map, StudyLanguageConstants.Italian, [StudyLanguageConstants.Italian, "it", "it-IT"]);

        return map;
    }

    private static IReadOnlyDictionary<string, string[]> BuildCanonicalToAliasesMap()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [StudyLanguageConstants.English] = [StudyLanguageConstants.English, "en", "en-US", "en-GB"],
            [StudyLanguageConstants.French] = [StudyLanguageConstants.French, "fr", "fr-FR"],
            [StudyLanguageConstants.German] = [StudyLanguageConstants.German, "de", "de-DE"],
            [StudyLanguageConstants.Portuguese] = [StudyLanguageConstants.Portuguese, "pt", "pt-PT", "pt-BR"],
            [StudyLanguageConstants.Spanish] = [StudyLanguageConstants.Spanish, "es", "es-ES", "es-MX"],
            [StudyLanguageConstants.Italian] = [StudyLanguageConstants.Italian, "it", "it-IT"]
        };

        return map;
    }

    private static void AddLanguageAliases(IDictionary<string, string> map, string canonical, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            map[alias] = canonical;
        }
    }
}
