namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSummaryGenerationService
{
    /// <summary>Best-effort generation. Implementations must never undo a completed lesson.</summary>
    Task TryGenerateForFinishedSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}
