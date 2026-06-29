using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public interface IAiModelSettingsService
{
    AiModelSettings GetActiveSettings();
    Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken);
    Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken);
    AiModelSettingsValidationResponse Validate(AiModelSettings settings);
    Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken);
    Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken);
}
