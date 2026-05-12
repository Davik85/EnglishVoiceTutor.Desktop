namespace EnglishVoiceTutor.Desktop.Constants;

public static class AudioConstants
{
    public const int MinimumRecordingDurationMilliseconds = 500;
    public const int MaximumRecordingDurationSeconds = 30;
    public const int TemporaryRecordingMaxAgeHours = 24;
    public const string AppTempFolderName = "EnglishVoiceTutor.Desktop";
    public const string RecordingFolderName = "Recordings";
    public const string BotVoiceFolderName = "BotVoice";
    public const string RecordingFilePrefix = "voice-recording-";
    public const string BotVoiceFilePrefix = "bot-voice-";
    public const string WavFileExtension = ".wav";
    public const string Mp3FileExtension = ".mp3";
    public const string WavSearchPattern = "*.wav";
    public const string Mp3SearchPattern = "*.mp3";
    public const string RecordingTimestampFormat = "yyyyMMdd-HHmmss-fff";
    public const string DefaultAudioInputDeviceId = "default";
    public const int DefaultAudioInputDeviceNumber = -1;
    public const string AudioInputDeviceIdPrefix = "device";
    public const string RecordingAlreadyInProgressMessage = "Recording is already in progress.";
    public const string RecordingTooShortMessage = "Recording too short. Please try again.";
    public const string RecordingTooLongMessage = "Recording is too long. Please keep voice answers under 30 seconds.";
    public const string BotVoicePlayingRecordingBlockedMessage = "Please wait until Elena finishes speaking.";
    public const string UnclearEnglishTranscriptionMessage = "I could not clearly recognize English. Please try recording again.";
}
