namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsContentImportService
{
    Task<CmsContentImportResult> ImportStaticContentAsync(Guid? actorUserId, CancellationToken cancellationToken);
}
