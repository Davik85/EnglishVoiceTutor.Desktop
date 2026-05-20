using EnglishVoiceTutor.Api.Contracts.LessonHistory;

namespace EnglishVoiceTutor.Api.Services;

public interface ILessonHistoryService
{
    Task<LessonHistoryListResponse> GetRecentDevLessonHistoryAsync(CancellationToken cancellationToken);
    Task<LessonHistoryDetailResponse?> GetDevLessonHistoryDetailAsync(Guid sessionId, CancellationToken cancellationToken);
}
