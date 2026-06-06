namespace EnglishVoiceTutor.Api.Services.Cms;

public interface ICmsRuntimeLessonContentService
{
    Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken);
}
