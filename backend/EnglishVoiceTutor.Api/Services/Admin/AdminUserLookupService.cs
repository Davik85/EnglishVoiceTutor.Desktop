using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminUserLookupService(
    AppDbContext dbContext,
    ISubscriptionStatusService subscriptionStatusService) : IAdminUserLookupService
{
    private const int RecentLessonSessionsLimit = 10;
    private const int DailyUsageCountersLimit = 14;
    private const int ActiveEntitlementsLimit = 10;
    private const int PremiumEntitlementScheduleLimit = 20;
    private const int RecentUsageEventsLimit = 20;

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

        return await BuildLookupResultAsync(user, cancellationToken);
    }

    public async Task<AdminUserLookupResult> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.Profile)
            .Include(candidate => candidate.Settings)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        return await BuildLookupResultAsync(user, cancellationToken);
    }

    private async Task<AdminUserLookupResult> BuildLookupResultAsync(UserEntity? user, CancellationToken cancellationToken)
    {
        if (user is null)
        {
            return new AdminUserLookupResult();
        }

        var subscriptionStatus = await subscriptionStatusService.GetStatusAsync(
            user.Id,
            AdminAuthorizationConstants.AdminUserLookupSource,
            cancellationToken);

        var recentLessonSessions = await dbContext.LessonSessions
            .AsNoTracking()
            .Where(session => session.UserId == user.Id)
            .OrderByDescending(session => session.StartedAt)
            .Take(RecentLessonSessionsLimit)
            .Select(session => new AdminUserLessonSessionSnapshot
            {
                SessionId = session.Id,
                LessonContentId = session.LessonContentId,
                StudyLanguage = session.StudyLanguage,
                TopicId = session.TopicId,
                TopicTitle = session.TopicTitle,
                SubtopicId = session.SubtopicId,
                SubtopicTitle = session.SubtopicTitle,
                Level = session.Level,
                SelectedContextId = session.SelectedContextId,
                SelectedContextTitle = session.SelectedContextTitle,
                ModeUsed = session.ModeUsed,
                Status = session.Status,
                StartedAt = session.StartedAt,
                FinishedAt = session.FinishedAt,
                ValidTurnCount = session.ValidTurnCount,
                EstimatedCost = session.EstimatedCost
            })
            .ToListAsync(cancellationToken);

        var dailyUsageCounters = await dbContext.DailyUsageCounters
            .AsNoTracking()
            .Where(counter => counter.UserId == user.Id)
            .OrderByDescending(counter => counter.UsageDate)
            .Take(DailyUsageCountersLimit)
            .Select(counter => new AdminUserDailyUsageCounterSnapshot
            {
                UsageDate = counter.UsageDate,
                StudyLanguage = counter.StudyLanguage,
                LessonsStarted = counter.LessonsStarted,
                LessonsCompleted = counter.LessonsCompleted,
                ChatReplyCount = counter.ChatReplyCount,
                HintsUsed = counter.HintsUsed,
                FeedbackRequests = counter.FeedbackRequests,
                TranscriptionSeconds = counter.TranscriptionSeconds,
                TtsSeconds = counter.TtsSeconds,
                EstimatedCost = counter.EstimatedCost,
                UpdatedAt = counter.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var activeEntitlements = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == user.Id)
            .Where(entitlement => entitlement.Status == SubscriptionConstants.Entitlements.StatusActive)
            .Where(entitlement => entitlement.StartsAtUtc <= now)
            .Where(entitlement => entitlement.ExpiresAtUtc == null || entitlement.ExpiresAtUtc > now)
            .OrderBy(entitlement => entitlement.ExpiresAtUtc == null)
            .ThenByDescending(entitlement => entitlement.ExpiresAtUtc)
            .Take(ActiveEntitlementsLimit)
            .Select(entitlement => new AdminUserEntitlementSnapshot
            {
                EntitlementId = entitlement.Id,
                PlanId = entitlement.PlanId,
                EntitlementType = entitlement.EntitlementType,
                Source = entitlement.Source,
                Status = entitlement.Status,
                StartsAtUtc = entitlement.StartsAtUtc,
                ExpiresAtUtc = entitlement.ExpiresAtUtc,
                Reason = entitlement.Reason,
                CreatedAt = entitlement.CreatedAt,
                UpdatedAt = entitlement.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var premiumEntitlementSchedule = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == user.Id)
            .Where(entitlement => entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType)
            .Where(entitlement => entitlement.Status == SubscriptionConstants.Entitlements.StatusActive)
            .Where(entitlement => entitlement.ExpiresAtUtc == null || entitlement.ExpiresAtUtc > now)
            .OrderBy(entitlement => entitlement.StartsAtUtc)
            .ThenBy(entitlement => entitlement.ExpiresAtUtc == null)
            .ThenBy(entitlement => entitlement.ExpiresAtUtc)
            .ThenBy(entitlement => entitlement.CreatedAt)
            .Take(PremiumEntitlementScheduleLimit)
            .Select(entitlement => new AdminUserEntitlementSnapshot
            {
                EntitlementId = entitlement.Id,
                PlanId = entitlement.PlanId,
                EntitlementType = entitlement.EntitlementType,
                Source = entitlement.Source,
                Status = entitlement.Status,
                StartsAtUtc = entitlement.StartsAtUtc,
                ExpiresAtUtc = entitlement.ExpiresAtUtc,
                Reason = entitlement.Reason,
                CreatedAt = entitlement.CreatedAt,
                UpdatedAt = entitlement.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var recentUsageEvents = await dbContext.UsageEvents
            .AsNoTracking()
            .Where(usageEvent => usageEvent.UserId == user.Id)
            .OrderByDescending(usageEvent => usageEvent.CreatedAt)
            .Take(RecentUsageEventsLimit)
            .Select(usageEvent => new AdminUserUsageEventSnapshot
            {
                UsageEventId = usageEvent.Id,
                SessionId = usageEvent.SessionId,
                Operation = usageEvent.Operation,
                Model = usageEvent.Model,
                StudyLanguage = usageEvent.StudyLanguage,
                Status = usageEvent.Status,
                InputTokens = usageEvent.InputTokens,
                OutputTokens = usageEvent.OutputTokens,
                AudioInputTokens = usageEvent.AudioInputTokens,
                AudioOutputTokens = usageEvent.AudioOutputTokens,
                AudioDurationMs = usageEvent.AudioDurationMs,
                InputChars = usageEvent.InputChars,
                OutputBytes = usageEvent.OutputBytes,
                EstimatedCost = usageEvent.EstimatedCost,
                CreatedAt = usageEvent.CreatedAt
            })
            .ToListAsync(cancellationToken);

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
                RecentLessonSessions = recentLessonSessions,
                DailyUsageCounters = dailyUsageCounters,
                ActiveEntitlements = activeEntitlements,
                PremiumEntitlementSchedule = premiumEntitlementSchedule,
                RecentUsageEvents = recentUsageEvents,
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
