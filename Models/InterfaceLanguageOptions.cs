using System.Globalization;
using EnglishVoiceTutor.Shared.NativeLanguages;

namespace EnglishVoiceTutor.Desktop.Models;

public static class InterfaceLanguageOptions
{
    public const string EnglishId = "en";
    public const string RussianId = "ru";
    public const string SpanishId = "es";
    public const string GermanId = "de";
    public const string FrenchId = "fr";
    public const string ItalianId = "it";
    public const string PortugueseId = "pt";
    public const string DefaultLanguageId = EnglishId;

    public static readonly InterfaceLanguageOption English = Create(NativeLanguageCatalog.English);
    public static readonly InterfaceLanguageOption Russian = Create(NativeLanguageCatalog.GetByIdOrName(RussianId));
    public static readonly InterfaceLanguageOption Spanish = Create(NativeLanguageCatalog.GetByIdOrName(SpanishId));
    public static readonly InterfaceLanguageOption German = Create(NativeLanguageCatalog.GetByIdOrName(GermanId));
    public static readonly InterfaceLanguageOption French = Create(NativeLanguageCatalog.GetByIdOrName(FrenchId));
    public static readonly InterfaceLanguageOption Italian = Create(NativeLanguageCatalog.GetByIdOrName(ItalianId));
    public static readonly InterfaceLanguageOption Portuguese = Create(NativeLanguageCatalog.GetByIdOrName(PortugueseId));

    public static readonly IReadOnlyList<string> ReleaseReadyInterfaceLanguageIds =
    [
        EnglishId,
        SpanishId,
        FrenchId,
        GermanId,
        ItalianId,
        PortugueseId,
        RussianId,
        "pl",
        "ar",
        "ja",
        "ko",
        "sr",
        "hr",
        "bg"
    ];

    public static readonly IReadOnlyList<InterfaceLanguageOption> All = ReleaseReadyInterfaceLanguageIds
        .Select(languageId => Create(NativeLanguageCatalog.GetByIdOrName(languageId)))
        .ToArray();

    public static InterfaceLanguageOption GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return English;
        }

        var language = NativeLanguageCatalog.GetByIdOrName(id);
        return All.FirstOrDefault(option => string.Equals(option.Id, language.Id, StringComparison.OrdinalIgnoreCase))
            ?? English;
    }

    public static InterfaceLanguageOption DetectFromCurrentCulture()
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        var culturePrefix = cultureName.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        return All.FirstOrDefault(option => string.Equals(option.CulturePrefix, culturePrefix, StringComparison.OrdinalIgnoreCase))
            ?? English;
    }

    private static InterfaceLanguageOption Create(NativeLanguageDefinition language)
    {
        return new InterfaceLanguageOption(
            language.Id,
            language.DisplayName,
            GetCulturePrefix(language.Id),
            language.EnglishName,
            language.IsRightToLeft);
    }

    private static string GetCulturePrefix(string languageId)
    {
        return languageId.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? languageId;
    }
}
