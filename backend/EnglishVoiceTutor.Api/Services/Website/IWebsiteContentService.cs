using EnglishVoiceTutor.Api.Contracts.Website;

namespace EnglishVoiceTutor.Api.Services.Website;

public interface IWebsiteContentService
{
    Task<WebsiteHomeHeaderResponse> GetAsync(CancellationToken cancellationToken);
    Task<WebsiteHomeHeaderResponse> SaveDraftAsync(WebsiteHomeHeaderContent draft, CancellationToken cancellationToken);
    Task<WebsitePublishResponse> PublishAsync(WebsiteHomeHeaderContent content, CancellationToken cancellationToken);
}
