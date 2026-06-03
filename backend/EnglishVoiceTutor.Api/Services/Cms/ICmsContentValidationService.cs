namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsContentValidationService
{
    CmsContentValidationResult Validate(CmsStaticContentImportDraft draft);
}
