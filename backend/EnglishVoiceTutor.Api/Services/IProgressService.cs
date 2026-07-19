using EnglishVoiceTutor.Api.Contracts.Progress;

namespace EnglishVoiceTutor.Api.Services;

public interface IProgressService
{
    Task<ProgressResponse> GetProgressAsync(CancellationToken cancellationToken);
}
