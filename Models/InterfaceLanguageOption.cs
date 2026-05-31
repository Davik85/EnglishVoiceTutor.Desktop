namespace EnglishVoiceTutor.Desktop.Models;

public sealed record InterfaceLanguageOption(
    string Id,
    string DisplayName,
    string CulturePrefix,
    string EnglishName,
    bool IsRightToLeft = false);
