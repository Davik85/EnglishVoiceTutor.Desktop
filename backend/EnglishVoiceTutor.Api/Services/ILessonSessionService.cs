using EnglishVoiceTutor.Api.Contracts.LessonSessions;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSessionService
{
    Task<LessonSessionResponse> StartLessonSessionAsync(StartLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionResponse> FinishLessonSessionAsync(Guid sessionId, FinishLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionListResponse> GetRecentLessonSessionsAsync(CancellationToken cancellationToken);
    Task<LessonSessionResponse?> GetLessonSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
}
