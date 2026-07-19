using EnglishVoiceTutor.Api.Contracts.Achievements;

namespace EnglishVoiceTutor.Api.Services;

public interface IAchievementsService
{
    Task<AchievementsResponse> GetAchievementsAsync(CancellationToken cancellationToken);
}
