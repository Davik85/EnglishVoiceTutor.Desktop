using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public interface IWebsiteCmsAdminMutationService
{
    Task<AdminWebsiteCmsSectionInitializationResponse> InitializeMissingSectionsAsync(CancellationToken cancellationToken);
}
