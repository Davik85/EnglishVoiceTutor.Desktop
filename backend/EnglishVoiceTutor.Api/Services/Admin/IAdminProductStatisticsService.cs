using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminProductStatisticsService
{
    Task<AdminProductStatisticsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
}
