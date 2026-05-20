namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonHistoryClientResult
{
    private BackendLessonHistoryClientResult(bool succeeded, IReadOnlyList<BackendLessonHistoryItemResponse> items, string? safeErrorMessage)
    {
        Succeeded = succeeded;
        Items = items;
        SafeErrorMessage = safeErrorMessage;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<BackendLessonHistoryItemResponse> Items { get; }

    public string? SafeErrorMessage { get; }

    public static BackendLessonHistoryClientResult Success(IReadOnlyList<BackendLessonHistoryItemResponse> items)
    {
        return new BackendLessonHistoryClientResult(true, items, null);
    }

    public static BackendLessonHistoryClientResult Failure(string safeErrorMessage)
    {
        return new BackendLessonHistoryClientResult(false, [], safeErrorMessage);
    }
}
