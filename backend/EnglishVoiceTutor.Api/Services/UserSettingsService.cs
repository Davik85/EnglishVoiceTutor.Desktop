using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public sealed class UserSettingsService(AppDbContext dbContext, DevUserProvider devUserProvider) : IUserSettingsService
{
    public const decimal MinSpeechSpeed = 0.5m;
    public const decimal MaxSpeechSpeed = 2.0m;

    private const string DefaultUserEmailPrefix = "user";
    private const string DefaultUserEmailDomain = "local.test";
    private const string DefaultUserPasswordHash = "temporary-dev-user-no-password-login";
    private const string DefaultUserStatus = "active";
    private const string DefaultDisplayName = "User";
    private const string DefaultNativeLanguage = "Russian";
    private const string DefaultCurrentLevel = "A1";
    private const string DefaultSelectedTutorId = "elena";
    private const string DefaultTimezone = "UTC";
    private const string DefaultExplanationLanguage = "Russian";
    private const string DefaultSpeechVoice = OpenAiConstants.DefaultSpeechVoice;
    private const decimal DefaultSpeechSpeed = 1.0m;
    private const bool DefaultConversationModeEnabled = true;

    public async Task<UserSettingsResponse> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await LoadOrCreateUserSettingsAsync(userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    public async Task<UserSettingsResponse> UpdateAsync(Guid userId, UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        ValidateUpdateRequest(request);

        var settings = await LoadOrCreateUserSettingsAsync(userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        settings.StudyLanguage = StudyLanguageConstants.ToCanonicalValue(request.StudyLanguage);
        settings.ExplanationLanguage = request.ExplanationLanguage.Trim();
        settings.SpeechVoice = request.SpeechVoice.Trim();
        settings.SpeechSpeed = request.SpeechSpeed;
        settings.ConversationModeEnabled = request.ConversationModeEnabled;
        settings.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(settings);
    }

    public Task<UserSettingsResponse> GetDevUserSettingsAsync(CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(devUserProvider.GetDevUserId(), cancellationToken);
    }

    public Task<UserSettingsResponse> UpdateDevUserSettingsAsync(UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        return UpdateAsync(devUserProvider.GetDevUserId(), request, cancellationToken);
    }

    private async Task<UserSettingsEntity> LoadOrCreateUserSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(existingUser => existingUser.Profile)
            .Include(existingUser => existingUser.Settings)
            .SingleOrDefaultAsync(existingUser => existingUser.Id == userId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (user is null)
        {
            user = new UserEntity
            {
                Id = userId,
                Email = BuildDefaultEmail(userId),
                PasswordHash = DefaultUserPasswordHash,
                Status = DefaultUserStatus,
                CreatedAt = now
            };

            dbContext.Users.Add(user);
        }

        if (user.Profile is null)
        {
            user.Profile = new UserProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = DefaultDisplayName,
                NativeLanguage = DefaultNativeLanguage,
                CurrentLevel = DefaultCurrentLevel,
                SelectedTutorId = DefaultSelectedTutorId,
                Timezone = DefaultTimezone,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.UserProfiles.Add(user.Profile);
        }

        if (user.Settings is null)
        {
            user.Settings = new UserSettingsEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StudyLanguage = StudyLanguageConstants.DefaultStudyLanguage,
                ExplanationLanguage = DefaultExplanationLanguage,
                SpeechVoice = DefaultSpeechVoice,
                SpeechSpeed = DefaultSpeechSpeed,
                ConversationModeEnabled = DefaultConversationModeEnabled,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.UserSettings.Add(user.Settings);
        }

        return user.Settings;
    }

    private static string BuildDefaultEmail(Guid userId)
    {
        return $"{DefaultUserEmailPrefix}-{userId:N}@{DefaultUserEmailDomain}";
    }

    private static void ValidateUpdateRequest(UpdateUserSettingsRequest request)
    {
        if (!StudyLanguageConstants.IsSupported(request.StudyLanguage))
        {
            throw new UserSettingsValidationException($"Study language must be one of: {string.Join(", ", StudyLanguageConstants.SupportedStudyLanguages)}.");
        }

        if (string.IsNullOrWhiteSpace(request.ExplanationLanguage))
        {
            throw new UserSettingsValidationException("Explanation language is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SpeechVoice))
        {
            throw new UserSettingsValidationException("Speech voice is required.");
        }

        if (request.SpeechSpeed is < MinSpeechSpeed or > MaxSpeechSpeed)
        {
            throw new UserSettingsValidationException($"Speech speed must be between {MinSpeechSpeed:0.0} and {MaxSpeechSpeed:0.0}.");
        }
    }

    private static UserSettingsResponse ToResponse(UserSettingsEntity settings)
    {
        return new UserSettingsResponse(
            settings.UserId,
            settings.StudyLanguage,
            settings.ExplanationLanguage,
            settings.SpeechVoice,
            settings.SpeechSpeed,
            settings.ConversationModeEnabled,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}
