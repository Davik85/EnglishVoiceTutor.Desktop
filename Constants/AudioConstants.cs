namespace EnglishVoiceTutor.Desktop.Constants;

public static class AudioConstants
{
    public const int MinimumRecordingDurationMilliseconds = 500;
    public const int MaximumRecordingDurationSeconds = 30;
    public const int TemporaryRecordingMaxAgeHours = 24;
    public const int BotVoiceCleanupRetentionHours = 24;
    public const int AutoPlayMaxCharacters = 300;
    public const int BotVoicePcmSampleRate = 24000;
    public const int BotVoicePcmBitsPerSample = 16;
    public const int BotVoicePcmChannels = 1;
    public const int BotVoiceStreamReadBufferBytes = 16384;
    public const string AppTempFolderName = "EnglishVoiceTutor.Desktop";
    public const string RecordingFolderName = "Recordings";
    public const string BotVoiceTempFolderName = "BotVoice";
    public const string BotVoiceFolderName = BotVoiceTempFolderName;
    public const string RecordingFilePrefix = "voice-recording-";
    public const string BotVoiceTempFilePrefix = "bot-voice-";
    public const string BotVoiceFilePrefix = BotVoiceTempFilePrefix;
    public const string WavFileExtension = ".wav";
    public const string Mp3FileExtension = ".mp3";
    public const string DefaultBotVoiceFileExtension = WavFileExtension;
    public const string WavSearchPattern = "*.wav";
    public const string Mp3SearchPattern = "*.mp3";
    public const string BotVoiceFileSearchPattern = "bot-voice-*";
    public const string RecordingTimestampFormat = "yyyyMMdd-HHmmss-fff";
    public const string DefaultAudioInputDeviceId = "default";
    public const int DefaultAudioInputDeviceNumber = -1;
    public const string AudioInputDeviceIdPrefix = "device";
    public const string RecordingAlreadyInProgressMessage = "Recording is already in progress.";
    public const string RecordingTooShortMessage = "Recording too short. Please try again.";
    public const string RecordingTooLongMessage = "Recording is too long. Please keep voice answers under 30 seconds.";
    public const string BotVoicePlayingRecordingBlockedMessage = "Please wait until Elena finishes speaking.";
    public const string BotVoiceCleanupErrorMessage = "Bot voice temporary file cleanup failed.";
    public const string UnclearEnglishTranscriptionMessage = "I could not clearly recognize English. Please try recording again.";
}
