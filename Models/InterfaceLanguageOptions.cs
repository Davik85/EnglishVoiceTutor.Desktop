using System.Globalization;

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

    public static readonly InterfaceLanguageOption English = new(EnglishId, "English", EnglishId);
    public static readonly InterfaceLanguageOption Russian = new(RussianId, "Русский", RussianId);
    public static readonly InterfaceLanguageOption Spanish = new(SpanishId, "Español", SpanishId);
    public static readonly InterfaceLanguageOption German = new(GermanId, "Deutsch", GermanId);
    public static readonly InterfaceLanguageOption French = new(FrenchId, "Français", FrenchId);
    public static readonly InterfaceLanguageOption Italian = new(ItalianId, "Italiano", ItalianId);
    public static readonly InterfaceLanguageOption Portuguese = new(PortugueseId, "Português", PortugueseId);

    public static readonly IReadOnlyList<InterfaceLanguageOption> All =
    [
        English,
        Russian,
        Spanish,
        German,
        French,
        Italian,
        Portuguese
    ];

    public static InterfaceLanguageOption GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return DetectFromCurrentCulture();
        }

        return All.FirstOrDefault(option => string.Equals(option.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? DetectFromCurrentCulture();
    }

    public static InterfaceLanguageOption DetectFromCurrentCulture()
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        var culturePrefix = cultureName.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        return All.FirstOrDefault(option => string.Equals(option.CulturePrefix, culturePrefix, StringComparison.OrdinalIgnoreCase))
            ?? English;
    }
}
