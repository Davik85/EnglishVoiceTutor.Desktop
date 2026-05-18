using EnglishVoiceTutor.Api.Contracts.UserSettings;

namespace EnglishVoiceTutor.Api.Services;

public interface IUserSettingsService
{
    Task<UserSettingsResponse> GetDevUserSettingsAsync(CancellationToken cancellationToken);
    Task<UserSettingsResponse> UpdateDevUserSettingsAsync(UpdateUserSettingsRequest request, CancellationToken cancellationToken);
}
