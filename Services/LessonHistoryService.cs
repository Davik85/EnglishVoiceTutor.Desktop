using System.IO;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.Auth;
using EnglishVoiceTutor.Desktop.Services.Auth;

namespace EnglishVoiceTutor.Desktop.Services;

public class LessonHistoryService
{
    private const int MaxHistoryItems = 20;
    private const string UserIdOwnerPrefix = "user:";
    private const string EmailOwnerPrefix = "email:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string historyFilePath;
    private readonly string[] historyFilePathCandidates;
    private readonly AuthSessionStorageService authSessionStorageService;

    public LessonHistoryService()
        : this(new AuthSessionStorageService())
    {
    }

    public LessonHistoryService(AuthSessionStorageService authSessionStorageService)
    {
        this.authSessionStorageService = authSessionStorageService;
        historyFilePath = LocalUserDataMigrationService.GetCurrentRoamingFilePath(StorageConstants.LessonHistoryFileName);
        historyFilePathCandidates = LocalUserDataMigrationService.BuildFilePathCandidates(StorageConstants.LessonHistoryFileName, includeLocalCurrentPath: false);
    }

    public string LessonHistoryFilePath => historyFilePath;

    public IReadOnlyList<string> LessonHistoryFilePathCandidates => historyFilePathCandidates;

    public IReadOnlyList<LessonHistoryItem> Load()
    {
        return LoadCompletedLessons();
    }

    public IReadOnlyList<LessonHistoryItem> LoadCompletedLessons(string? selectedLevel = null)
    {
        return LoadCompletedLessonsForOwner(ownerKey: null, includeLegacyOwnerlessRecords: true, selectedLevel);
    }

    public async Task<IReadOnlyList<LessonHistoryItem>> LoadVisibleCompletedLessonsForCurrentSessionAsync(
        string? selectedLevel = null,
        CancellationToken cancellationToken = default)
    {
        // Signed-in current-session history uses owner aliases and keeps includeLegacyOwnerlessRecords: false semantics.
        var ownerKeys = await GetCurrentOwnerKeysAsync(cancellationToken);
        if (ownerKeys.Count == 0)
        {
            return [];
        }

        return LoadCompletedLessonsForOwnerKeys(ownerKeys, selectedLevel);
    }

    public IReadOnlyList<LessonHistoryItem> LoadCompletedLessonsForOwner(
        string? ownerKey,
        bool includeLegacyOwnerlessRecords,
        string? selectedLevel = null)
    {
        var normalizedOwnerKey = NormalizeOwnerKey(ownerKey);
        var items = LoadRawItems()
            .Where(IsCompletedLessonRecord)
            .Where(item => IsVisibleForOwner(item, normalizedOwnerKey, includeLegacyOwnerlessRecords))
            .GroupBy(item => item.Id == Guid.Empty ? BuildFallbackHistoryKey(item) : item.Id.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.CompletedAt).First())
            .OrderByDescending(item => item.CompletedAt)
            .Take(MaxHistoryItems)
            .ToList();

        if (!string.IsNullOrWhiteSpace(selectedLevel))
        {
            items = items
                .Where(item => string.Equals(item.SelectedLevel, selectedLevel, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return items;
    }


    public IReadOnlyList<LessonHistoryItem> LoadCompletedLessonsForOwnerKeys(
        IReadOnlyCollection<string> ownerKeys,
        string? selectedLevel = null)
    {
        var normalizedOwnerKeys = ownerKeys
            .Select(NormalizeOwnerKey)
            .Where(ownerKey => !string.IsNullOrWhiteSpace(ownerKey))
            .Select(ownerKey => ownerKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (normalizedOwnerKeys.Count == 0)
        {
            return [];
        }

        var items = LoadRawItems()
            .Where(IsCompletedLessonRecord)
            .Where(item => IsVisibleForAnyOwner(item, normalizedOwnerKeys))
            .GroupBy(item => item.Id == Guid.Empty ? BuildFallbackHistoryKey(item) : item.Id.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.CompletedAt).First())
            .OrderByDescending(item => item.CompletedAt)
            .Take(MaxHistoryItems)
            .ToList();

        if (!string.IsNullOrWhiteSpace(selectedLevel))
        {
            items = items
                .Where(item => string.Equals(item.SelectedLevel, selectedLevel, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return items;
    }

    public int CountCompletedLessons(string? selectedLevel = null)
    {
        return LoadCompletedLessons(selectedLevel).Count;
    }

    public async Task AddForCurrentSessionAsync(LessonHistoryItem item, CancellationToken cancellationToken = default)
    {
        var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
        ApplyOwner(item, session?.User);
        Add(item);
    }

    public void Add(LessonHistoryItem item)
    {
        if (!IsCompletedLessonRecord(item))
        {
            return;
        }

        try
        {
            var items = LoadRawItems()
                .Where(IsCompletedLessonRecord)
                .ToList();
            items.RemoveAll(existing => IsSameHistoryRecord(existing, item));
            items.Insert(0, item);

            var directoryPath = Path.GetDirectoryName(historyFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = JsonSerializer.Serialize(items, JsonOptions);
            File.WriteAllText(historyFilePath, json);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string? BuildOwnerKey(AuthUserDto? user)
    {
        if (user is null)
        {
            return null;
        }

        if (user.UserId != Guid.Empty)
        {
            return UserIdOwnerPrefix + user.UserId.ToString("D");
        }

        var normalizedEmail = NormalizeEmail(user.Email);
        return string.IsNullOrWhiteSpace(normalizedEmail) ? null : EmailOwnerPrefix + normalizedEmail;
    }

    public static string? NormalizeOwnerKey(string? ownerKey)
    {
        return string.IsNullOrWhiteSpace(ownerKey) ? null : ownerKey.Trim().ToLowerInvariant();
    }

    public static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
    }

    private async Task<IReadOnlyCollection<string>> GetCurrentOwnerKeysAsync(CancellationToken cancellationToken)
    {
        var session = await authSessionStorageService.GetValidSessionOrNullAsync(cancellationToken);
        return BuildOwnerKeyAliases(session?.User);
    }

    private static IReadOnlyCollection<string> BuildOwnerKeyAliases(AuthUserDto? user)
    {
        if (user is null)
        {
            return [];
        }

        var ownerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var primaryOwnerKey = BuildOwnerKey(user);
        if (!string.IsNullOrWhiteSpace(primaryOwnerKey))
        {
            ownerKeys.Add(primaryOwnerKey);
        }

        var normalizedEmail = NormalizeEmail(user.Email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            ownerKeys.Add(EmailOwnerPrefix + normalizedEmail);
        }

        return ownerKeys;
    }

    private static void ApplyOwner(LessonHistoryItem item, AuthUserDto? user)
    {
        var ownerKey = BuildOwnerKey(user);
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            item.OwnerUserId = null;
            item.OwnerEmail = null;
            item.OwnerKey = null;
            return;
        }

        item.OwnerUserId = user?.UserId == Guid.Empty ? null : user?.UserId;
        item.OwnerEmail = NormalizeEmail(user?.Email);
        item.OwnerKey = ownerKey;
    }

    private IReadOnlyList<LessonHistoryItem> LoadRawItems()
    {
        if (!File.Exists(historyFilePath))
        {
            LocalUserDataMigrationService.CopyFirstLegacyFileToCurrentWhenMissing(historyFilePath, historyFilePathCandidates, "lesson history");
        }

        if (!File.Exists(historyFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(historyFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var items = JsonSerializer.Deserialize<List<LessonHistoryItem>>(json, JsonOptions);
            return items ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool IsVisibleForOwner(LessonHistoryItem item, string? ownerKey, bool includeLegacyOwnerlessRecords)
    {
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return includeLegacyOwnerlessRecords;
        }

        var itemOwnerKey = GetItemOwnerKey(item);
        return string.Equals(itemOwnerKey, ownerKey, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVisibleForAnyOwner(LessonHistoryItem item, IReadOnlySet<string> ownerKeys)
    {
        var itemOwnerKey = GetItemOwnerKey(item);
        return !string.IsNullOrWhiteSpace(itemOwnerKey) && ownerKeys.Contains(itemOwnerKey);
    }

    private static string? GetItemOwnerKey(LessonHistoryItem item)
    {
        var storedOwnerKey = NormalizeOwnerKey(item.OwnerKey);
        if (!string.IsNullOrWhiteSpace(storedOwnerKey))
        {
            return storedOwnerKey;
        }

        if (item.OwnerUserId is Guid ownerUserId && ownerUserId != Guid.Empty)
        {
            return UserIdOwnerPrefix + ownerUserId.ToString("D");
        }

        var normalizedEmail = NormalizeEmail(item.OwnerEmail);
        return string.IsNullOrWhiteSpace(normalizedEmail) ? null : EmailOwnerPrefix + normalizedEmail;
    }

    private static bool IsCompletedLessonRecord(LessonHistoryItem? item)
    {
        if (item is null || item.CompletedAt == default)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(item.TopicTitle)
            || !string.IsNullOrWhiteSpace(item.SubtopicTitle)
            || !string.IsNullOrWhiteSpace(item.GoodText)
            || !string.IsNullOrWhiteSpace(item.ImproveText)
            || (item.UsefulPhrases?.Count ?? 0) > 0;
    }

    private static bool IsSameHistoryRecord(LessonHistoryItem left, LessonHistoryItem right)
    {
        if (left.Id != Guid.Empty && right.Id != Guid.Empty)
        {
            return left.Id == right.Id;
        }

        return string.Equals(BuildFallbackHistoryKey(left), BuildFallbackHistoryKey(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFallbackHistoryKey(LessonHistoryItem item)
    {
        return string.Join(
            '|',
            GetItemOwnerKey(item) ?? "legacy",
            item.CompletedAt.ToUniversalTime().Ticks.ToString(),
            item.SelectedLevel.Trim(),
            item.TopicTitle.Trim(),
            item.SubtopicTitle.Trim());
    }
}
