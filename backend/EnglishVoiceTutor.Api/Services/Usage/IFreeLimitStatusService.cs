using EnglishVoiceTutor.Api.Contracts.Usage;

namespace EnglishVoiceTutor.Api.Services.Usage;

public interface IFreeLimitStatusService
{
    Task<FreeLimitStatusResponse> GetDevFreeLimitStatusAsync(string? studyLanguage, CancellationToken cancellationToken);
}
