using EnglishVoiceTutor.Api.Contracts.Cms;

namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsContentPublishingService
{
    Task<CmsContentVersionListResponse?> ListVersionsAsync(string slug, CancellationToken cancellationToken);
    Task<CmsContentVersionResponse?> GetVersionAsync(string slug, int versionNumber, CancellationToken cancellationToken);
    Task<PublishCmsContentResponse?> PublishDraftAsync(string slug, PublishCmsContentRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<RestoreCmsContentVersionResponse?> RestoreVersionAsync(string slug, int versionNumber, RestoreCmsContentVersionRequest request, Guid actorUserId, CancellationToken cancellationToken);
}
