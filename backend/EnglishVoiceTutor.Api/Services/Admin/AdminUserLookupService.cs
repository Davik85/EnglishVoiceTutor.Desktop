using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminUserLookupService(
    AppDbContext dbContext,
    ISubscriptionStatusService subscriptionStatusService) : IAdminUserLookupService
{
    public async Task<AdminUserLookupResult> GetByEmailAsync(string? email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return new AdminUserLookupResult
            {
                IsInvalidEmail = true
            };
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.Profile)
            .Include(candidate => candidate.Settings)
            .SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new AdminUserLookupResult();
        }

        var subscriptionStatus = await subscriptionStatusService.GetStatusAsync(
            user.Id,
            AdminAuthorizationConstants.AdminUserLookupSource,
            cancellationToken);

        return new AdminUserLookupResult
        {
            Response = new AdminUserLookupResponse
            {
                User = new AdminUserSnapshot
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                },
                Profile = user.Profile is null
                    ? null
                    : new AdminUserProfileSnapshot
                    {
                        DisplayName = user.Profile.DisplayName,
                        NativeLanguage = user.Profile.NativeLanguage,
                        CurrentLevel = user.Profile.CurrentLevel,
                        SelectedTutorId = user.Profile.SelectedTutorId,
                        Timezone = user.Profile.Timezone
                    },
                Settings = user.Settings is null
                    ? null
                    : new AdminUserSettingsSnapshot
                    {
                        StudyLanguage = user.Settings.StudyLanguage,
                        ExplanationLanguage = user.Settings.ExplanationLanguage,
                        SpeechVoice = user.Settings.SpeechVoice,
                        SpeechSpeed = user.Settings.SpeechSpeed,
                        ConversationModeEnabled = user.Settings.ConversationModeEnabled
                    },
                SubscriptionStatus = subscriptionStatus,
                CheckedAtUtc = DateTimeOffset.UtcNow
            }
        };
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        return email.Trim().ToLowerInvariant();
    }
}
