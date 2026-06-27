using EnglishVoiceTutor.Api.Contracts.Website;

namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public interface IWebsiteCmsPublicReadService
{
    Task<WebsiteTextsResponse> GetPublicTextsAsync(CancellationToken cancellationToken);
}
