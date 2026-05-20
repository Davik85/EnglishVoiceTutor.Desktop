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
        var backendResult = await backendLessonHistoryClient.GetHistoryAsync(backendBaseUrl);
        if (backendResult.Succeeded)
        {
            var mappedBackendItems = backendResult.Items
                .Where(item => string.Equals(item.Level, selectedLevel, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.FinishedAt ?? item.StartedAt)
                .Select(MapBackendItem)
                .ToList();

            ReplaceItems(mappedBackendItems);
            return;
        }

        var localItems = lessonHistoryService
            .Load()
            .Where(item => string.Equals(item.SelectedLevel, selectedLevel, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ReplaceItems(localItems);
    }

    private static LessonHistoryItem MapBackendItem(BackendLessonHistoryItemResponse item)
    {
        return new LessonHistoryItem
        {
            Id = item.SessionId,
            CompletedAt = (item.FinishedAt ?? item.StartedAt).LocalDateTime,
            SelectedLevel = item.Level,
            TopicTitle = item.TopicTitle,
            SubtopicTitle = item.SubtopicTitle,
            GoodText = item.SummaryPreview ?? string.Empty,
            ImproveText = string.Empty,
            UsefulPhrases = []
        };
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
