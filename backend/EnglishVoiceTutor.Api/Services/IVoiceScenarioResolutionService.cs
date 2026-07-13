using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public interface IVoiceScenarioResolutionService
{
    Task<VoiceScenarioResolutionResponse> ResolveAsync(
        VoiceScenarioResolutionRequest request,
        CancellationToken cancellationToken = default);
}
