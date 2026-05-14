using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Desktop.Services.Voice;

public interface IVoiceConversationEngine
{
    Task StartSessionAsync(VoiceSessionStartRequest request, CancellationToken cancellationToken);
    Task SendUserTextAsync(string text, CancellationToken cancellationToken);
    Task StartUserAudioAsync(CancellationToken cancellationToken);
    Task AppendUserAudioAsync(ReadOnlyMemory<byte> audioChunk, CancellationToken cancellationToken);
    Task CommitUserAudioAsync(CancellationToken cancellationToken);
    Task StopSessionAsync(CancellationToken cancellationToken);
    event EventHandler<AssistantAudioChunkReceivedEventArgs>? AssistantAudioChunkReceived;
    event EventHandler<AssistantTranscriptDeltaEventArgs>? AssistantTranscriptDeltaReceived;
    event EventHandler<AssistantTurnCompletedEventArgs>? AssistantTurnCompleted;
    event EventHandler<UserAudioCommittedEventArgs>? UserAudioCommitted;
    event EventHandler<UserTranscriptDeltaEventArgs>? UserTranscriptDeltaReceived;
    event EventHandler<UserTranscriptCompletedEventArgs>? UserTranscriptCompleted;
    event EventHandler<VoiceSessionErrorEventArgs>? ErrorReceived;
}

public sealed record VoiceSessionStartRequest
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public string TutorProfileId { get; init; } = string.Empty;
    public string TutorDisplayName { get; init; } = string.Empty;
    public string SelectedLevel { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string TopicTitle { get; init; } = string.Empty;
    public string Subtopic { get; init; } = string.Empty;
    public string SubtopicTitle { get; init; } = string.Empty;
    public string LessonScenarioId { get; init; } = string.Empty;
    public string LessonType { get; init; } = string.Empty;
    public string LessonGoal { get; init; } = string.Empty;
    public string LessonPhase { get; init; } = string.Empty;
    public string CurrentPhase { get; init; } = string.Empty;
    public string TutorRole { get; init; } = string.Empty;
    public string UserRole { get; init; } = string.Empty;
    public string Situation { get; init; } = string.Empty;
    public string TargetLanguageName { get; init; } = "English";
    public string NativeLanguageName { get; init; } = string.Empty;
    public string UserDisplayName { get; init; } = string.Empty;
    public string LearningGoal { get; init; } = string.Empty;
    public string SelectedContextVariantId { get; init; } = string.Empty;
    public string SelectedContextTitle { get; init; } = string.Empty;
    public string SelectedContextOpeningLine { get; init; } = string.Empty;
    public string LastBotMessage { get; init; } = string.Empty;
    public int LearnerTurnCount { get; init; }
    public int SoftLearnerTurnLimit { get; init; }
    public int HardLearnerTurnLimit { get; init; }
    public IReadOnlyList<string> TargetLanguageKeyPhrases { get; init; } = [];
    public IReadOnlyList<string> GrammarFocus { get; init; } = [];
    public string FeedbackRulesSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> AiTutorPromptInstructions { get; init; } = [];
    public LevelProfile ActiveLevelProfile { get; init; } = new();
    public IReadOnlyList<RecentConversationMessage> RecentMessages { get; init; } = [];
}

public sealed class AssistantAudioChunkReceivedEventArgs : EventArgs
{
    public AssistantAudioChunkReceivedEventArgs(string sessionId, string responseId, byte[] audioChunk, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ResponseId = responseId;
        AudioChunk = audioChunk;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ResponseId { get; }
    public byte[] AudioChunk { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class AssistantTranscriptDeltaEventArgs : EventArgs
{
    public AssistantTranscriptDeltaEventArgs(string sessionId, string responseId, string delta, string transcriptSoFar, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ResponseId = responseId;
        Delta = delta;
        TranscriptSoFar = transcriptSoFar;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ResponseId { get; }
    public string Delta { get; }
    public string TranscriptSoFar { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class AssistantTurnCompletedEventArgs : EventArgs
{
    public AssistantTurnCompletedEventArgs(string sessionId, string responseId, string transcript, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ResponseId = responseId;
        Transcript = transcript;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ResponseId { get; }
    public string Transcript { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class VoiceSessionErrorEventArgs : EventArgs
{
    public VoiceSessionErrorEventArgs(string sessionId, string message, string? responseId = null, Exception? exception = null)
    {
        SessionId = sessionId;
        Message = message;
        ResponseId = responseId;
        Exception = exception;
    }

    public string SessionId { get; }
    public string? ResponseId { get; }
    public string Message { get; }
    public Exception? Exception { get; }
}

public sealed class UserAudioCommittedEventArgs : EventArgs
{
    public UserAudioCommittedEventArgs(string sessionId, string itemId, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ItemId = itemId;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ItemId { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class UserTranscriptDeltaEventArgs : EventArgs
{
    public UserTranscriptDeltaEventArgs(string sessionId, string itemId, string delta, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ItemId = itemId;
        Delta = delta;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ItemId { get; }
    public string Delta { get; }
    public long ElapsedMilliseconds { get; }
}

public sealed class UserTranscriptCompletedEventArgs : EventArgs
{
    public UserTranscriptCompletedEventArgs(string sessionId, string itemId, string transcript, long elapsedMilliseconds)
    {
        SessionId = sessionId;
        ItemId = itemId;
        Transcript = transcript;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public string SessionId { get; }
    public string ItemId { get; }
    public string Transcript { get; }
    public long ElapsedMilliseconds { get; }
}
