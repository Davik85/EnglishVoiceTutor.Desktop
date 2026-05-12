namespace EnglishVoiceTutor.Api.Services;

public sealed class AudioSpeechRequestCanceledException : TaskCanceledException
{
    public AudioSpeechRequestCanceledException(
        string message,
        Exception? innerException,
        bool internalTimeoutReached,
        bool clientCancellationRequested)
        : base(message, innerException)
    {
        InternalTimeoutReached = internalTimeoutReached;
        ClientCancellationRequested = clientCancellationRequested;
    }

    public bool InternalTimeoutReached { get; }

    public bool ClientCancellationRequested { get; }
}
