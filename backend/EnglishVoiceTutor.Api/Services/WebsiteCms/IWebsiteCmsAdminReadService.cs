using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public interface IWebsiteCmsAdminReadService
{
    Task<AdminWebsiteCmsSectionOverviewResponse> GetSectionOverviewAsync(CancellationToken cancellationToken);
    Task<AdminWebsiteCmsSectionDetailResponse?> GetSectionDetailAsync(string sectionKey, CancellationToken cancellationToken);
}
