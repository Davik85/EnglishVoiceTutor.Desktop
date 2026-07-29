namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsContentValidationService
{
    CmsContentValidationResult Validate(CmsStaticContentImportDraft draft);
    Task<CmsContentValidationResult> ValidateDraftRowsAsync(Guid contentPackId, CancellationToken cancellationToken);
    Task<CmsContentValidationResult> ValidateDraftRowsForPublicationAsync(Guid contentPackId, CancellationToken cancellationToken);
}
