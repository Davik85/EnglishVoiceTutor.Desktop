namespace EnglishVoiceTutor.Desktop.Localization;

public static class DiagnosticsLocalization
{
    public static DiagnosticsLocalizedText GetText(string? languageId)
    {
        var appText = AppLocalization.GetText(languageId);
        var settings = appText.Settings;

        return new DiagnosticsLocalizedText(
            settings.DiagnosticsTitle,
            "Technical information for troubleshooting.",
            "App version",
            "Backend URL",
            "Backend status",
            "Database status",
            "AI status",
            "Settings file",
            "Lesson history file",
            settings.InterfaceLanguageTitle,
            settings.NativeLanguageTitle,
            settings.TutorAvatarTitle,
            settings.RefreshStatusButtonText,
            "Copy diagnostics",
            "Diagnostics copied.",
            "Could not copy diagnostics.",
            "Healthy",
            "unavailable",
            "checking...",
            "configured",
            "not configured",
            "unknown");
    }
}
