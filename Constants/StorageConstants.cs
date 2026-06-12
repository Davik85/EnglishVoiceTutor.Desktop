namespace EnglishVoiceTutor.Desktop.Constants;

public static class StorageConstants
{
    public const string AppDataFolderName = "LanguageVoiceTutor.Desktop";
    public const string StableAppDataFolderName = AppDataFolderName;
    public const string SettingsFileName = "settings.json";
    public const string LessonHistoryFileName = "lesson-history.json";
    public const string AuthSessionFileName = "auth-session.json";
    public const string BackendRequestDiagnosticsFileName = "backend-request-diagnostics.log";

    public static readonly string[] LegacyAppDataFolderNames =
    [
        "EnglishVoiceTutor.Desktop",
        "Language Voice Tutor"
    ];
}
