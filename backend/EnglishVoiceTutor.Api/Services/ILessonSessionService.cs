using EnglishVoiceTutor.Api.Contracts.LessonSessions;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSessionService
{
    Task<LessonSessionResponse> StartLessonSessionAsync(StartLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionResponse> FinishLessonSessionAsync(Guid sessionId, FinishLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionResponse> RecordLessonSessionHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<LessonSessionResponse> AbandonLessonSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<LessonSessionResponse?> AbandonActiveLessonSessionAsync(CancellationToken cancellationToken);
    Task<LessonSessionListResponse> GetRecentLessonSessionsAsync(CancellationToken cancellationToken);
    Task<LessonSessionResponse?> GetLessonSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
}
