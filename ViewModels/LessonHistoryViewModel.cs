using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Localization;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonHistoryViewModel : ViewModelBase
{
    private readonly Action navigateBack;
    private readonly AppLocalizedText localizedText;

    public string Title => localizedText.LessonHistoryTitle;

    public string Subtitle => localizedText.LessonHistorySubtitle;

    public string EmptyHistoryText => localizedText.EmptyLessonHistoryText;

    public string BackButtonText => localizedText.BackToTopicsText;

    public string GoodLabel => localizedText.WhatWentWellTitle;

    public string ImproveLabel => localizedText.WhatToImproveTitle;

    public string TopicLabel => localizedText.TopicContextLabel;

    public string SituationLabel => localizedText.SituationContextLabel;

    public string LevelLabel => localizedText.LevelContextLabel;

    public ObservableCollection<LessonHistoryItem> Items { get; }

    public bool HasHistory => Items.Count > 0;

    public LessonHistoryViewModel(
        AppLocalizedText localizedText,
        LessonHistoryService lessonHistoryService,
        BackendLessonHistoryClient backendLessonHistoryClient,
        string? backendBaseUrl,
        string selectedLevel,
        Action navigateBack)
    {
        this.localizedText = localizedText;
        this.navigateBack = navigateBack;
        Items = new ObservableCollection<LessonHistoryItem>();

        _ = LoadHistoryAsync(lessonHistoryService, backendLessonHistoryClient, backendBaseUrl, selectedLevel);
    }

    private async Task LoadHistoryAsync(
        LessonHistoryService lessonHistoryService,
        BackendLessonHistoryClient backendLessonHistoryClient,
        string? backendBaseUrl,
        string selectedLevel)
    {
        _ = backendLessonHistoryClient;
        _ = backendBaseUrl;

        var localItems = await lessonHistoryService.LoadVisibleCompletedLessonsForCurrentSessionAsync(selectedLevel);
        if (localItems.Count > 0)
        {
            ReplaceItems(localItems);
            return;
        }

        var backendResult = await backendLessonHistoryClient.GetHistoryAsync(backendBaseUrl);
        ReplaceItems(backendResult.Succeeded ? MapBackendItems(backendResult.Items, selectedLevel) : localItems);
    }


    private static IReadOnlyList<LessonHistoryItem> MapBackendItems(IReadOnlyList<BackendLessonHistoryItemResponse> backendItems, string selectedLevel)
    {
        return backendItems
            .Where(item => string.Equals(item.Status, "completed", StringComparison.OrdinalIgnoreCase) || item.FinishedAt.HasValue)
            .Where(item => string.IsNullOrWhiteSpace(selectedLevel) || string.Equals(item.Level, selectedLevel, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.FinishedAt ?? item.UpdatedAt)
            .Select(item => new LessonHistoryItem
            {
                Id = item.SessionId,
                CompletedAt = (item.FinishedAt ?? item.UpdatedAt).LocalDateTime,
                SelectedLevel = item.Level,
                TopicTitle = item.TopicTitle,
                SubtopicTitle = item.SubtopicTitle,
                GoodText = item.SummaryPreview ?? string.Empty,
                ImproveText = string.Empty,
                UsefulPhrases = []
            })
            .ToList();
    }

    private void ReplaceItems(IReadOnlyList<LessonHistoryItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(nameof(HasHistory));
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
