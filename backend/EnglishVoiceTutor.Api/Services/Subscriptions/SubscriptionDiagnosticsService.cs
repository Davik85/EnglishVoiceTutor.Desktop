using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.SubscriptionDiagnostics;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class SubscriptionDiagnosticsService(
    AppDbContext dbContext,
    ISubscriptionStatusService subscriptionStatusService,
    ISubscriptionPlanCatalogService subscriptionPlanCatalogService) : ISubscriptionDiagnosticsService
{
    public async Task<SubscriptionDiagnosticScenarioResponse> ApplyScenarioAsync(string scenario, Guid userId, string source, CancellationToken cancellationToken)
    {
        var normalizedScenario = scenario.Trim().ToLowerInvariant();

        await subscriptionPlanCatalogService.EnsureDefaultPlansAsync(cancellationToken);

        switch (normalizedScenario)
        {
            case SubscriptionConstants.Diagnostics.ScenarioReset:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                break;
            case SubscriptionConstants.Diagnostics.ScenarioActivePremiumEntitlement:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                await AddPremiumEntitlementAsync(userId, false);
                break;
            case SubscriptionConstants.Diagnostics.ScenarioActiveTrialGrant:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                await AddTrialGrantAsync(userId, false);
                break;
            case SubscriptionConstants.Diagnostics.ScenarioExpiredPremiumEntitlement:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                await AddPremiumEntitlementAsync(userId, true);
                break;
            case SubscriptionConstants.Diagnostics.ScenarioExpiredTrialGrant:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                await AddTrialGrantAsync(userId, true);
                break;
            case SubscriptionConstants.Diagnostics.ScenarioDailyFreeLessonConsumed:
                await ResetDiagnosticRecordsAsync(userId, cancellationToken);
                await AddDailyFreeLessonUsageAsync(userId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown diagnostics scenario.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var status = await subscriptionStatusService.GetStatusAsync(userId, source, cancellationToken);
        return new SubscriptionDiagnosticScenarioResponse
        {
            Scenario = normalizedScenario,
            AppliedTo = userId.ToString(),
            AppliedAtUtc = DateTimeOffset.UtcNow,
            Status = status
        };
    }

    private async Task ResetDiagnosticRecordsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        var diagnosticEntitlements = await dbContext.Entitlements
            .Where(entitlement => entitlement.UserId == userId && entitlement.Reason == SubscriptionConstants.Diagnostics.Reason)
            .ToListAsync(cancellationToken);

        var diagnosticTrialGrants = await dbContext.TrialGrants
            .Where(grant => grant.UserId == userId && grant.SourcePlatform == SubscriptionConstants.Diagnostics.SourcePlatform)
            .ToListAsync(cancellationToken);

        var todayDailyUsage = await dbContext.DailyFreeLessonUsages
            .Where(usage => usage.UserId == userId && usage.UsageDate == todayUtc)
            .Join(
                dbContext.LessonSessions,
                usage => usage.LessonSessionId,
                session => session.Id,
                (usage, session) => new { usage, session })
            .ToListAsync(cancellationToken);

        var diagnosticDailyUsage = todayDailyUsage
            .Where(pair => pair.session.LessonContentId == SubscriptionConstants.Diagnostics.LessonContentId)
            .ToList();

        dbContext.Entitlements.RemoveRange(diagnosticEntitlements);
        dbContext.TrialGrants.RemoveRange(diagnosticTrialGrants);
        dbContext.DailyFreeLessonUsages.RemoveRange(todayDailyUsage.Select(pair => pair.usage));
        dbContext.LessonSessions.RemoveRange(diagnosticDailyUsage.Select(pair => pair.session));
    }

    private Task AddPremiumEntitlementAsync(Guid userId, bool expired)
    {
        var now = DateTimeOffset.UtcNow;
        var startsAt = expired
            ? now.AddDays(-SubscriptionConstants.Diagnostics.ExpiredPremiumStartOffsetDays)
            : now.AddMinutes(-SubscriptionConstants.Diagnostics.ActivePremiumStartOffsetMinutes);
        var expiresAt = expired
            ? now.AddDays(-SubscriptionConstants.Diagnostics.ExpiredOffsetDays)
            : now.AddDays(SubscriptionConstants.Diagnostics.ActivePremiumDurationDays);

        dbContext.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceManualAdmin,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = startsAt,
            ExpiresAtUtc = expiresAt,
            Reason = SubscriptionConstants.Diagnostics.Reason,
            CreatedAt = now,
            UpdatedAt = now
        });

        return Task.CompletedTask;
    }

    private Task AddTrialGrantAsync(Guid userId, bool expired)
    {
        var now = DateTimeOffset.UtcNow;
        var grantedAt = expired ? now.AddDays(-SubscriptionConstants.Diagnostics.ExpiredTrialGrantOffsetDays) : now;
        var expiresAt = expired ? now.AddDays(-SubscriptionConstants.Diagnostics.ExpiredOffsetDays) : now.AddDays(SubscriptionConstants.PremiumTrialDays);

        dbContext.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = grantedAt,
            ExpiresAtUtc = expiresAt,
            SourcePlatform = SubscriptionConstants.Diagnostics.SourcePlatform,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = now
        });

        return Task.CompletedTask;
    }

    private Task AddDailyFreeLessonUsageAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var lessonSessionId = Guid.NewGuid();

        dbContext.LessonSessions.Add(new LessonSessionEntity
        {
            Id = lessonSessionId,
            UserId = userId,
            LessonContentId = SubscriptionConstants.Diagnostics.LessonContentId,
            StudyLanguage = StudyLanguageConstants.DefaultStudyLanguage,
            TopicId = SubscriptionConstants.Diagnostics.TopicId,
            TopicTitle = SubscriptionConstants.Diagnostics.TopicTitle,
            SubtopicId = SubscriptionConstants.Diagnostics.SubtopicId,
            SubtopicTitle = SubscriptionConstants.Diagnostics.SubtopicTitle,
            Level = SubscriptionConstants.Diagnostics.Level,
            SelectedContextId = SubscriptionConstants.Diagnostics.ContextId,
            SelectedContextTitle = SubscriptionConstants.Diagnostics.ContextTitle,
            ModeUsed = SubscriptionConstants.Diagnostics.Mode,
            Status = SubscriptionConstants.Diagnostics.SessionStatus,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        dbContext.DailyFreeLessonUsages.Add(new DailyFreeLessonUsageEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UsageDate = DateOnly.FromDateTime(now.UtcDateTime),
            StudyLanguage = StudyLanguageConstants.DefaultStudyLanguage,
            LessonSessionId = lessonSessionId,
            UserMessageCountAtConsumption = SubscriptionConstants.FreeLessonConsumptionMessageThreshold,
            ConsumedAtUtc = now,
            CreatedAt = now
        });

        return Task.CompletedTask;
    }
}
