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

    public IReadOnlyList<LessonHistoryItem> Load()
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

    public void Add(LessonHistoryItem item)
    {
        var items = Load().ToList();
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
}
