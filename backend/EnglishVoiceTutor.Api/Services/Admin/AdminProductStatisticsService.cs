using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Usage;
using EnglishVoiceTutor.Shared.NativeLanguages;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminProductStatisticsService(AppDbContext dbContext) : IAdminProductStatisticsService
{
    private const int ActivityWindowDays = 30;
    private const string UnknownLanguage = "Unknown";

    private static readonly UsageStudyLanguageNormalizer StudyLanguageNormalizer = new();

    private static readonly IReadOnlyDictionary<string, string> MetricDefinitions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["totalInstallations"] = "Tracked authenticated device records from backend DeviceEntity rows, upserted by user plus coarse platform, display device name, and app version. This is not raw installer download count.",
        ["registeredUsersTotal"] = "Total backend account records from UserEntity rows. No emails or personal data are returned.",
        ["activeTrialsNow"] = "Distinct users with active trial grants at checkedAtUtc: active status, granted at or before checkedAtUtc, and expiring after checkedAtUtc.",
        ["activeUsersLast30Days"] = "Distinct users with a lesson session started or usage event created during the last 30 days.",
        ["activePremiumUsersNow"] = "Distinct users with active Premium access entitlements at checkedAtUtc: Premium plan/access type, active status, started, and not expired.",
        ["activeFreeUsersLast30Days"] = "Distinct users active in the last 30 days who do not currently have active Premium and do not currently have active Trial; this is an inferred free-user category.",
        ["studyLanguageDistribution"] = "Backward-compatible alias of selectedStudyLanguageDistribution.",
        ["selectedStudyLanguageDistribution"] = "Users grouped by current selected study language from user settings. Unknown means users without a stored study-language value.",
        ["practicedStudyLanguageDistributionLast30Days"] = "Distinct active users grouped by study language used in lesson sessions or usage events during the last 30 days. Unknown means activity without a stored study-language value.",
        ["nativeLanguageDistribution"] = "Users grouped by profile native language only. Unknown means the profile native language is missing, empty, or explicitly unknown.",
        ["explanationLanguageDistribution"] = "Users grouped by settings explanation language. This is a rough interface/content localization signal and is not the same as native language. Unknown means the explanation language is missing, empty, or explicitly unknown."
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
        var selectedStudyLanguageDistribution = await GetSelectedStudyLanguageDistributionAsync(cancellationToken);
        var practicedStudyLanguageDistributionLast30Days = await GetPracticedStudyLanguageDistributionLast30DaysAsync(windowStartUtc, cancellationToken);
        var nativeLanguageDistribution = await GetNativeLanguageDistributionAsync(cancellationToken);
        var explanationLanguageDistribution = await GetExplanationLanguageDistributionAsync(cancellationToken);

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
            StudyLanguageDistribution = selectedStudyLanguageDistribution,
            SelectedStudyLanguageDistribution = selectedStudyLanguageDistribution,
            PracticedStudyLanguageDistributionLast30Days = practicedStudyLanguageDistributionLast30Days,
            NativeLanguageDistribution = nativeLanguageDistribution,
            ExplanationLanguageDistribution = explanationLanguageDistribution,
            Definitions = MetricDefinitions
        };
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetSelectedStudyLanguageDistributionAsync(CancellationToken cancellationToken)
    {
        var languages = await dbContext.UserSettings
            .AsNoTracking()
            .Select(settings => settings.StudyLanguage)
            .ToListAsync(cancellationToken);

        return BuildDistribution(GroupLanguageCounts(languages));
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetPracticedStudyLanguageDistributionLast30DaysAsync(DateTimeOffset windowStartUtc, CancellationToken cancellationToken)
    {
        var lessonLanguages = await dbContext.LessonSessions
            .AsNoTracking()
            .Where(session => session.StartedAt >= windowStartUtc)
            .Select(session => new { session.StudyLanguage, session.UserId })
            .ToListAsync(cancellationToken);

        var usageLanguages = await dbContext.UsageEvents
            .AsNoTracking()
            .Where(usageEvent => usageEvent.CreatedAt >= windowStartUtc)
            .Select(usageEvent => new { usageEvent.StudyLanguage, usageEvent.UserId })
            .ToListAsync(cancellationToken);

        var uniqueLanguageUsers = new HashSet<LanguageUserPair>();
        foreach (var item in lessonLanguages)
        {
            var normalizedLanguage = NormalizeLanguageForStatistics(item.StudyLanguage);
            uniqueLanguageUsers.Add(new LanguageUserPair(normalizedLanguage, item.UserId));
        }

        foreach (var item in usageLanguages)
        {
            var normalizedLanguage = NormalizeLanguageForStatistics(item.StudyLanguage);
            uniqueLanguageUsers.Add(new LanguageUserPair(normalizedLanguage, item.UserId));
        }

        return BuildDistribution(GroupLanguageUserCounts(uniqueLanguageUsers));
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetNativeLanguageDistributionAsync(CancellationToken cancellationToken)
    {
        var languages = await dbContext.Users
            .AsNoTracking()
            .Select(user => user.Profile == null ? null : user.Profile.NativeLanguage)
            .ToListAsync(cancellationToken);

        return BuildDistribution(GroupLanguageCounts(languages));
    }

    private async Task<IReadOnlyList<AdminLanguageDistributionItem>> GetExplanationLanguageDistributionAsync(CancellationToken cancellationToken)
    {
        var languages = await dbContext.UserSettings
            .AsNoTracking()
            .Select(settings => settings.ExplanationLanguage)
            .ToListAsync(cancellationToken);

        return BuildDistribution(GroupLanguageCounts(languages));
    }

    private static IReadOnlyList<LanguageDistributionCount> GroupLanguageCounts(IEnumerable<string?> languages)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var rawLanguage in languages)
        {
            var normalizedLanguage = NormalizeLanguageForStatistics(rawLanguage);
            counts[normalizedLanguage] = counts.TryGetValue(normalizedLanguage, out var current)
                ? current + 1
                : 1;
        }

        return ToLanguageDistributionCounts(counts);
    }

    private static IReadOnlyList<LanguageDistributionCount> GroupLanguageUserCounts(IEnumerable<LanguageUserPair> languageUsers)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var languageUser in languageUsers)
        {
            var normalizedLanguage = NormalizeLanguageForStatistics(languageUser.Language);
            counts[normalizedLanguage] = counts.TryGetValue(normalizedLanguage, out var current)
                ? current + 1
                : 1;
        }

        return ToLanguageDistributionCounts(counts);
    }

    private static IReadOnlyList<LanguageDistributionCount> ToLanguageDistributionCounts(IReadOnlyDictionary<string, int> counts)
    {
        return counts
            .Select(pair => new LanguageDistributionCount(pair.Key, pair.Value))
            .ToList();
    }

    private static string NormalizeMissingLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) ? UnknownLanguage : language.Trim();
    }

    private static IReadOnlyList<AdminLanguageDistributionItem> BuildDistribution(
        IReadOnlyList<LanguageDistributionCount> groupedLanguages)
    {
        var normalizedGroupCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in groupedLanguages)
        {
            var normalizedLanguage = NormalizeLanguageForStatistics(group.Language);
            normalizedGroupCounts[normalizedLanguage] = normalizedGroupCounts.TryGetValue(normalizedLanguage, out var current)
                ? current + group.UserCount
                : group.UserCount;
        }

        var normalizedGroups = ToLanguageDistributionCounts(normalizedGroupCounts);
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

    private static string NormalizeLanguageForStatistics(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UnknownLanguage;
        }

        var trimmed = language.Trim();
        if (string.Equals(trimmed, UnknownLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return UnknownLanguage;
        }

        var catalogLanguage = NativeLanguageCatalog.All.FirstOrDefault(item =>
            string.Equals(item.Id, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.EnglishName, trimmed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase));

        if (catalogLanguage is not null)
        {
            return catalogLanguage.EnglishName;
        }

        var normalizedStudyLanguage = StudyLanguageNormalizer.NormalizeOrDefault(trimmed, UnknownLanguage);

        return StudyLanguageConstants.IsSupported(normalizedStudyLanguage)
            ? StudyLanguageConstants.ToCanonicalValue(normalizedStudyLanguage)
            : normalizedStudyLanguage;
    }

    private sealed record LanguageDistributionCount(string Language, int UserCount);
    private sealed record LanguageUserPair(string Language, Guid UserId);
}
