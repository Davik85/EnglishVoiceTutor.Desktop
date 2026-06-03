namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsPublishedContentService
{
    Task<CmsPublishedContentReadResult> ReadLatestPublishedContentAsync(CancellationToken cancellationToken);
}
