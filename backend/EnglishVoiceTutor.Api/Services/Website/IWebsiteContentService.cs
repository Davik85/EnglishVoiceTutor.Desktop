using EnglishVoiceTutor.Api.Contracts.Website;

namespace EnglishVoiceTutor.Api.Services.Website;

public interface IWebsiteContentService
{
    Task<WebsiteContentResponse> GetAsync(CancellationToken cancellationToken);
    Task<WebsiteContentResponse> SaveDraftAsync(WebsiteContentSet draft, CancellationToken cancellationToken);
    Task<WebsitePublishResponse> PublishAsync(WebsiteContentSet content, CancellationToken cancellationToken);
}
