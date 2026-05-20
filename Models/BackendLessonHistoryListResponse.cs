namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonHistoryListResponse
{
    public IReadOnlyList<BackendLessonHistoryItemResponse> Items { get; set; } = [];
}
