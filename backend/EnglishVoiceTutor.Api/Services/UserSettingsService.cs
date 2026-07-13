using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Shared.NativeLanguages;
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
    private const string DefaultNativeLanguage = NativeLanguageCatalog.DefaultLanguageId;
    private const string DefaultCurrentLevel = "A1";
    private const string DefaultSelectedTutorId = "lana";
    private const string DefaultTimezone = "UTC";
    private const string DefaultExplanationLanguage = NativeLanguageCatalog.DefaultLanguageId;
    private const string DefaultSpeechVoice = OpenAiConstants.DefaultSpeechVoice;
    private const decimal DefaultSpeechSpeed = 1.0m;
    private const bool DefaultConversationModeEnabled = true;

    public async Task<UserSettingsResponse> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadOrCreateUserAsync(userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserSettingsResponse> UpdateAsync(Guid userId, UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        ValidateUpdateRequest(request);

        var user = await LoadOrCreateUserAsync(userId, cancellationToken);
        var settings = user.Settings!;
        var profile = user.Profile!;
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NativeLanguage))
        {
            profile.NativeLanguage = NativeLanguageCatalog.GetByIdOrName(request.NativeLanguage).Id;
            profile.UpdatedAt = now;
        }

        if (request.SelectedTutorId is not null)
        {
            profile.SelectedTutorId = ToSupportedCanonicalTutorId(request.SelectedTutorId);
            profile.UpdatedAt = now;
        }

        if (request.CurrentLevel is not null)
        {
            profile.CurrentLevel = ToSupportedCanonicalLevel(request.CurrentLevel);
            profile.UpdatedAt = now;
        }

        settings.StudyLanguage = StudyLanguageConstants.ToCanonicalValue(request.StudyLanguage);
        settings.ExplanationLanguage = NativeLanguageCatalog.GetByIdOrName(request.ExplanationLanguage).Id;
        settings.SpeechVoice = request.SpeechVoice.Trim();
        settings.SpeechSpeed = request.SpeechSpeed;
        settings.ConversationModeEnabled = request.ConversationModeEnabled;
        settings.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public Task<UserSettingsResponse> GetDevUserSettingsAsync(CancellationToken cancellationToken)
    {
        return GetOrCreateAsync(devUserProvider.GetDevUserId(), cancellationToken);
    }

    public Task<UserSettingsResponse> UpdateDevUserSettingsAsync(UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        return UpdateAsync(devUserProvider.GetDevUserId(), request, cancellationToken);
    }

    private async Task<UserEntity> LoadOrCreateUserAsync(Guid userId, CancellationToken cancellationToken)
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
        else
        {
            var canonicalSelectedTutorId = TutorAvatarOptions.GetById(user.Profile.SelectedTutorId).Id;
            if (!string.Equals(user.Profile.SelectedTutorId, canonicalSelectedTutorId, StringComparison.Ordinal))
            {
                user.Profile.SelectedTutorId = canonicalSelectedTutorId;
                user.Profile.UpdatedAt = now;
            }

            string? canonicalCurrentLevel = null;
            if (TryGetSupportedCanonicalLevel(user.Profile.CurrentLevel, out var supportedCurrentLevel))
            {
                canonicalCurrentLevel = supportedCurrentLevel;
            }
            else if (IsNonMeaningfulLegacyCurrentLevel(user.Profile.CurrentLevel))
            {
                canonicalCurrentLevel = DefaultCurrentLevel;
            }

            if (canonicalCurrentLevel is not null
                && !string.Equals(user.Profile.CurrentLevel, canonicalCurrentLevel, StringComparison.Ordinal))
            {
                user.Profile.CurrentLevel = canonicalCurrentLevel;
                user.Profile.UpdatedAt = now;
            }
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

        return user;
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

        if (!string.IsNullOrWhiteSpace(request.NativeLanguage) && !NativeLanguageCatalog.IsSupported(request.NativeLanguage))
        {
            throw new UserSettingsValidationException("Native language must be a supported language code or name.");
        }

        if (!NativeLanguageCatalog.IsSupported(request.ExplanationLanguage))
        {
            throw new UserSettingsValidationException("Explanation language must be a supported native/interface/explanation language code or name.");
        }

        if (request.SelectedTutorId is not null)
        {
            _ = ToSupportedCanonicalTutorId(request.SelectedTutorId);
        }

        if (request.CurrentLevel is not null)
        {
            _ = ToSupportedCanonicalLevel(request.CurrentLevel);
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

    private static string ToSupportedCanonicalTutorId(string? tutorId)
    {
        var canonicalTutorId = TutorAvatarOptions.ToCanonicalId(tutorId);
        var supportedTutor = TutorAvatarOptions.All.FirstOrDefault(option => string.Equals(option.Id, canonicalTutorId, StringComparison.OrdinalIgnoreCase));
        if (supportedTutor is not null)
        {
            return supportedTutor.Id;
        }

        throw new UserSettingsValidationException($"Selected tutor must be one of: {string.Join(", ", TutorAvatarOptions.All.Select(option => option.Id))}.");
    }

    private static string ToSupportedCanonicalLevel(string level)
    {
        if (TryGetSupportedCanonicalLevel(level, out var supportedLevel))
        {
            return supportedLevel;
        }

        throw new UserSettingsValidationException(
            $"Current level must be one of: {string.Join(", ", CmsLevelProfiles.RequiredLevelKeys.Select(key => key.ToUpperInvariant()))}.");
    }

    private static bool TryGetSupportedCanonicalLevel(string? level, out string canonicalLevel)
    {
        var trimmedLevel = level?.Trim();
        var supportedLevel = CmsLevelProfiles.RequiredLevelKeys.FirstOrDefault(
            key => string.Equals(key, trimmedLevel, StringComparison.OrdinalIgnoreCase));
        canonicalLevel = supportedLevel?.ToUpperInvariant() ?? string.Empty;
        return supportedLevel is not null;
    }

    private static bool IsNonMeaningfulLegacyCurrentLevel(string? level)
    {
        return string.IsNullOrWhiteSpace(level)
            || string.Equals(level.Trim(), "unknown", StringComparison.OrdinalIgnoreCase);
    }

    private static UserSettingsResponse ToResponse(UserEntity user)
    {
        var settings = user.Settings ?? throw new InvalidOperationException("User settings are required.");
        var profile = user.Profile ?? throw new InvalidOperationException("User profile is required.");
        var nativeLanguage = string.IsNullOrWhiteSpace(profile.NativeLanguage)
            ? DefaultNativeLanguage
            : profile.NativeLanguage;
        var selectedTutorId = TutorAvatarOptions.GetById(profile.SelectedTutorId).Id;
        var currentLevel = TryGetSupportedCanonicalLevel(profile.CurrentLevel, out var supportedCurrentLevel)
            ? supportedCurrentLevel
            : DefaultCurrentLevel;

        return new UserSettingsResponse(
            settings.UserId,
            nativeLanguage,
            settings.StudyLanguage,
            settings.ExplanationLanguage,
            currentLevel,
            selectedTutorId,
            settings.SpeechVoice,
            settings.SpeechSpeed,
            settings.ConversationModeEnabled,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}
