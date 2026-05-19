using EnglishVoiceTutor.Api.Contracts.LessonSessions;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSessionService
{
    Task<LessonSessionResponse> StartDevLessonSessionAsync(StartLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionResponse> FinishDevLessonSessionAsync(Guid sessionId, FinishLessonSessionRequest request, CancellationToken cancellationToken);
    Task<LessonSessionListResponse> GetRecentDevLessonSessionsAsync(CancellationToken cancellationToken);
    Task<LessonSessionResponse?> GetDevLessonSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
}
