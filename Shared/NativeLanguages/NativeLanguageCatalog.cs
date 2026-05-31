namespace EnglishVoiceTutor.Shared.NativeLanguages;

public static class NativeLanguageCatalog
{
    public const string Tier1 = "Tier 1";
    public const string Tier2 = "Tier 2";
    public const string EuropeanAddOn = "European add-on";
    public const string DefaultLanguageId = "en";

    public static readonly NativeLanguageDefinition English = new(DefaultLanguageId, "English", "English", Tier1);

    public static readonly IReadOnlyList<NativeLanguageDefinition> All =
    [
        English,
        new("es", "Spanish", "Español", Tier1),
        new("fr", "French", "Français", Tier1),
        new("de", "German", "Deutsch", Tier1),
        new("it", "Italian", "Italiano", Tier1),
        new("pt", "Portuguese", "Português", Tier1),
        new("ru", "Russian", "Русский", Tier1),
        new("uk", "Ukrainian", "Українська", Tier1),
        new("pl", "Polish", "Polski", Tier1),
        new("nl", "Dutch", "Nederlands", Tier1),
        new("tr", "Turkish", "Türkçe", Tier1),
        new("ar", "Arabic", "العربية", Tier1, IsRightToLeft: true),
        new("hi", "Hindi", "हिन्दी", Tier1),
        new("zh-Hans", "Chinese Simplified", "简体中文", Tier1),
        new("ja", "Japanese", "日本語", Tier1),
        new("ko", "Korean", "한국어", Tier1),
        new("vi", "Vietnamese", "Tiếng Việt", Tier1),
        new("id", "Indonesian", "Bahasa Indonesia", Tier1),
        new("fa", "Persian", "فارسی", Tier2, IsRightToLeft: true),
        new("ur", "Urdu", "اردو", Tier2, IsRightToLeft: true),
        new("bn", "Bengali", "বাংলা", Tier2),
        new("ta", "Tamil", "தமிழ்", Tier2),
        new("te", "Telugu", "తెలుగు", Tier2),
        new("mr", "Marathi", "मराठी", Tier2),
        new("gu", "Gujarati", "ગુજરાતી", Tier2),
        new("th", "Thai", "ไทย", Tier2),
        new("sv", "Swedish", "Svenska", Tier2),
        new("no", "Norwegian", "Norsk", Tier2),
        new("da", "Danish", "Dansk", Tier2),
        new("cs", "Czech", "Čeština", Tier2),
        new("ro", "Romanian", "Română", Tier2),
        new("el", "Greek", "Ελληνικά", Tier2),
        new("he", "Hebrew", "עברית", Tier2, IsRightToLeft: true),
        new("sr", "Serbian", "Српски", EuropeanAddOn),
        new("hr", "Croatian", "Hrvatski", EuropeanAddOn),
        new("bs", "Bosnian", "Bosanski", EuropeanAddOn),
        new("sl", "Slovenian", "Slovenščina", EuropeanAddOn),
        new("sk", "Slovak", "Slovenčina", EuropeanAddOn),
        new("bg", "Bulgarian", "Български", EuropeanAddOn),
        new("hu", "Hungarian", "Magyar", EuropeanAddOn),
        new("fi", "Finnish", "Suomi", EuropeanAddOn),
        new("et", "Estonian", "Eesti", EuropeanAddOn),
        new("lv", "Latvian", "Latviešu", EuropeanAddOn),
        new("lt", "Lithuanian", "Lietuvių", EuropeanAddOn),
        new("sq", "Albanian", "Shqip", EuropeanAddOn),
        new("mk", "Macedonian", "Македонски", EuropeanAddOn),
        new("be", "Belarusian", "Беларуская", EuropeanAddOn),
        new("is", "Icelandic", "Íslenska", EuropeanAddOn),
        new("ga", "Irish", "Gaeilge", EuropeanAddOn),
        new("cy", "Welsh", "Cymraeg", EuropeanAddOn),
        new("ca", "Catalan", "Català", EuropeanAddOn),
        new("eu", "Basque", "Euskara", EuropeanAddOn),
        new("gl", "Galician", "Galego", EuropeanAddOn),
        new("mt", "Maltese", "Malti", EuropeanAddOn),
        new("lb", "Luxembourgish", "Lëtzebuergesch", EuropeanAddOn)
    ];

    public static NativeLanguageDefinition GetByIdOrName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return English;
        }

        var trimmed = value.Trim();
        return All.FirstOrDefault(language =>
                string.Equals(language.Id, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.EnglishName, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase))
            ?? English;
    }

    public static bool IsSupported(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && All.Any(language =>
                string.Equals(language.Id, value.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.EnglishName, value.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.DisplayName, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
