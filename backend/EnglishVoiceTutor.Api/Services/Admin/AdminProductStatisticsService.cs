using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminProductStatisticsService(AppDbContext dbContext) : IAdminProductStatisticsService
{
    private const int ActivityWindowDays = 30;

    private static readonly IReadOnlyDictionary<string, string> MetricDefinitions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["totalInstallations"] = "Tracked installation/device records from backend DeviceEntity rows; this is not raw installer download count.",
        ["registeredUsersTotal"] = "Total backend account records from UserEntity rows. No emails or personal data are returned.",
        ["activeTrialsNow"] = "Distinct users with active trial grants at checkedAtUtc: active status, granted at or before checkedAtUtc, and expiring after checkedAtUtc.",
        ["activeUsersLast30Days"] = "Distinct users with a lesson session started or usage event created during the last 30 days.",
        ["activePremiumUsersNow"] = "Distinct users with active Premium access entitlements at checkedAtUtc: Premium plan/access type, active status, started, and not expired.",
        ["activeFreeUsersLast30Days"] = "Distinct users active in the last 30 days who do not currently have active Premium and do not currently have active Trial; this is an inferred free-user category."
    };

    public async Task<AdminProductStatisticsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;
        var windowStartUtc = checkedAtUtc.AddDays(-ActivityWindowDays);

        var activeTrialUserIds = dbContext.TrialGrants
            .AsNoTracking()
            .Where(trial => trial.Status == SubscriptionConstants.Entitlements.StatusActive
                && trial.GrantedAtUtc <= checkedAtUtc
                && trial.ExpiresAtUtc > checkedAtUtc)
            .Select(trial => trial.UserId)
            .Distinct();

        var activePremiumUserIds = dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.PlanId == SubscriptionConstants.Plans.PremiumPlanId
                && entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType
                && entitlement.Status == SubscriptionConstants.Entitlements.StatusActive
                && entitlement.StartsAtUtc <= checkedAtUtc
                && (entitlement.ExpiresAtUtc == null || entitlement.ExpiresAtUtc > checkedAtUtc))
            .Select(entitlement => entitlement.UserId)
            .Distinct();

        var activeUserIdsLast30Days = dbContext.LessonSessions
            .AsNoTracking()
            .Where(session => session.StartedAt >= windowStartUtc)
            .Select(session => session.UserId)
            .Union(dbContext.UsageEvents
                .AsNoTracking()
                .Where(usageEvent => usageEvent.CreatedAt >= windowStartUtc)
                .Select(usageEvent => usageEvent.UserId))
            .Distinct();

        var activeFreeUserIdsLast30Days = activeUserIdsLast30Days
            .Except(activePremiumUserIds)
            .Except(activeTrialUserIds);

        var totalInstallations = await dbContext.Devices.AsNoTracking().CountAsync(cancellationToken);
        var registeredUsersTotal = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var activeTrialsNow = await activeTrialUserIds.CountAsync(cancellationToken);
        var activeUsersLast30Days = await activeUserIdsLast30Days.CountAsync(cancellationToken);
        var activePremiumUsersNow = await activePremiumUserIds.CountAsync(cancellationToken);
        var activeFreeUsersLast30Days = await activeFreeUserIdsLast30Days.CountAsync(cancellationToken);

        return new AdminProductStatisticsOverviewResponse
        {
            CheckedAtUtc = checkedAtUtc,
            WindowDays = ActivityWindowDays,
            WindowStartUtc = windowStartUtc,
            TotalInstallations = totalInstallations,
            RegisteredUsersTotal = registeredUsersTotal,
            ActiveTrialsNow = activeTrialsNow,
            ActiveUsersLast30Days = activeUsersLast30Days,
            ActivePremiumUsersNow = activePremiumUsersNow,
            ActiveFreeUsersLast30Days = activeFreeUsersLast30Days,
            Definitions = MetricDefinitions
        };
    }
}
