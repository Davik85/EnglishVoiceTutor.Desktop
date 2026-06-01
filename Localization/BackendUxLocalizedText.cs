namespace EnglishVoiceTutor.Desktop.Localization;

public sealed record BackendUxLocalizedText(
    string BackendUnavailable,
    string CouldNotConnect,
    string ActionNeedsBackend,
    string BackendReturnedError,
    string BackendValidationError,
    string BackendRequestTimedOut,
    string BackendUnexpectedResponse,
    string SettingsLoadUnavailable,
    string SettingsSaveUnavailable,
    string LoginFailed,
    string RegisterFailed,
    string SignedIn,
    string SignedOut,
    string SessionRestored,
    string SessionExpired,
    string CredentialsRequired,
    string DisplayNameRequired,
    string VoiceTakingTooLong,
    string VoicePlaybackUnavailable);
