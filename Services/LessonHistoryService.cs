using System.IO;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public class LessonHistoryService
{
    private const int MaxHistoryItems = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string historyFilePath;

    public LessonHistoryService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        historyFilePath = Path.Combine(appDataPath, StorageConstants.AppDataFolderName, StorageConstants.LessonHistoryFileName);
    }

    public string LessonHistoryFilePath => historyFilePath;

    public IReadOnlyList<LessonHistoryItem> Load()
    {
        return LoadCompletedLessons();
    }

    public IReadOnlyList<LessonHistoryItem> LoadCompletedLessons(string? selectedLevel = null)
    {
        var items = LoadRawItems()
            .Where(IsCompletedLessonRecord)
            .GroupBy(item => item.Id == Guid.Empty ? BuildFallbackHistoryKey(item) : item.Id.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.CompletedAt).First())
            .OrderByDescending(item => item.CompletedAt)
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

    public void Add(LessonHistoryItem item)
    {
        if (!IsCompletedLessonRecord(item))
        {
            return;
        }

        try
        {
            var items = LoadCompletedLessons().ToList();
            items.RemoveAll(existing => IsSameHistoryRecord(existing, item));
            items.Insert(0, item);

            if (items.Count > MaxHistoryItems)
            {
                items = items.Take(MaxHistoryItems).ToList();
            }

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

    private IReadOnlyList<LessonHistoryItem> LoadRawItems()
    {
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
            item.CompletedAt.ToUniversalTime().Ticks.ToString(),
            item.SelectedLevel.Trim(),
            item.TopicTitle.Trim(),
            item.SubtopicTitle.Trim());
    }
}
