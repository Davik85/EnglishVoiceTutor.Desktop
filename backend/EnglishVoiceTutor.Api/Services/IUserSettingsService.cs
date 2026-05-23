using EnglishVoiceTutor.Api.Contracts.UserSettings;

namespace EnglishVoiceTutor.Api.Services;

public interface IUserSettingsService
{
    Task<UserSettingsResponse> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserSettingsResponse> UpdateAsync(Guid userId, UpdateUserSettingsRequest request, CancellationToken cancellationToken);
    Task<UserSettingsResponse> GetDevUserSettingsAsync(CancellationToken cancellationToken);
    Task<UserSettingsResponse> UpdateDevUserSettingsAsync(UpdateUserSettingsRequest request, CancellationToken cancellationToken);
}
