using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Shared.NativeLanguages;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminProductStatisticsService(AppDbContext dbContext) : IAdminProductStatisticsService
{
    private const int ActivityWindowDays = 30;
    private const string UnknownLanguage = "Unknown";

    private static readonly IReadOnlyDictionary<string, string> MetricDefinitions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["totalInstallations"] = "Tracked installation/device records from backend DeviceEntity rows; this is not raw installer download count.",
        ["registeredUsersTotal"] = "Total backend account records from UserEntity rows. No emails or personal data are returned.",
        ["activeTrialsNow"] = "Distinct users with active trial grants at checkedAtUtc: active status, granted at or before checkedAtUtc, and expiring after checkedAtUtc.",
        ["activeUsersLast30Days"] = "Distinct users with a lesson session started or usage event created during the last 30 days.",
        ["activePremiumUsersNow"] = "Distinct users with active Premium access entitlements at checkedAtUtc: Premium plan/access type, active status, started, and not expired.",
        ["activeFreeUsersLast30Days"] = "Distinct users active in the last 30 days who do not currently have active Premium and do not currently have active Trial; this is an inferred free-user category.",
        ["studyLanguageDistribution"] = "Users grouped by selected study language from user settings. Unknown means users without a stored study-language value.",
        ["nativeLanguageDistribution"] = "Users grouped by native language from user profile. Unknown means users without a stored native-language value."
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
        var studyLanguageDistribution = await GetStudyLanguageDistributionAsync(cancellationToken);
        var nativeLanguageDistribution = await GetNativeLanguageDistributionAsync(cancellationToken);

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
            StudyLanguageDistribution = studyLanguageDistribution,
            NativeLanguageDistribution = nativeLanguageDistribution,
            Definitions = MetricDefinitions
        };
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetStudyLanguageDistributionAsync(CancellationToken cancellationToken)
    {
        var groupedLanguages = await dbContext.UserSettings
            .AsNoTracking()
            .GroupBy(settings => settings.StudyLanguage == null || settings.StudyLanguage.Trim() == string.Empty
                ? UnknownLanguage
                : settings.StudyLanguage.Trim())
            .Select(group => new LanguageDistributionCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        return BuildDistribution(groupedLanguages, NormalizeStudyLanguage);
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetNativeLanguageDistributionAsync(CancellationToken cancellationToken)
    {
        var groupedLanguages = await dbContext.UserProfiles
            .AsNoTracking()
            .GroupBy(profile => profile.NativeLanguage == null || profile.NativeLanguage.Trim() == string.Empty
                ? UnknownLanguage
                : profile.NativeLanguage.Trim())
            .Select(group => new LanguageDistributionCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        return BuildDistribution(groupedLanguages, NormalizeNativeLanguage);
    }

    private static IReadOnlyList<AdminLanguageDistributionItem> BuildDistribution(
        IReadOnlyList<LanguageDistributionCount> groupedLanguages,
        Func<string, string> normalizeLanguage)
    {
        var normalizedGroups = groupedLanguages
            .GroupBy(group => normalizeLanguage(group.Language), StringComparer.Ordinal)
            .Select(group => new LanguageDistributionCount(group.Key, group.Sum(item => item.UserCount)))
            .ToList();
        var totalUsers = normalizedGroups.Sum(group => group.UserCount);

        return normalizedGroups
            .OrderByDescending(group => group.UserCount)
            .ThenBy(group => group.Language, StringComparer.Ordinal)
            .Select(group => new AdminLanguageDistributionItem
            {
                Language = group.Language,
                UserCount = group.UserCount,
                Percentage = totalUsers == 0
                    ? 0m
                    : Math.Round(group.UserCount * 100m / totalUsers, 1, MidpointRounding.AwayFromZero)
            })
            .ToList();
    }

    private static string NormalizeStudyLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UnknownLanguage;
        }

        return StudyLanguageConstants.IsSupported(language)
            ? StudyLanguageConstants.ToCanonicalValue(language)
            : language.Trim();
    }

    private static string NormalizeNativeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UnknownLanguage;
        }

        var trimmed = language.Trim();
        var nativeLanguage = NativeLanguageCatalog.All.FirstOrDefault(item =>
            string.Equals(item.Id, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.EnglishName, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase));

        return nativeLanguage?.EnglishName ?? trimmed;
    }

    private sealed record LanguageDistributionCount(string Language, int UserCount);
}
