using EnglishVoiceTutor.Api.Contracts.LessonSummaries;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonSummaryService
{
    Task<LessonSummaryResponse> UpsertDevLessonSummaryAsync(Guid sessionId, UpsertLessonSummaryRequest request, CancellationToken cancellationToken);
    Task<LessonSummaryResponse> GetDevLessonSummaryAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<LessonSummaryListResponse> GetRecentDevLessonSummariesAsync(CancellationToken cancellationToken);
    Task<AuthenticatedLessonSummaryResponse?> GetAuthenticatedLessonSummaryAsync(Guid sessionId, CancellationToken cancellationToken);
}
