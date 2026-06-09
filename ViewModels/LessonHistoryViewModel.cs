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

    private Task LoadHistoryAsync(
        LessonHistoryService lessonHistoryService,
        BackendLessonHistoryClient backendLessonHistoryClient,
        string? backendBaseUrl,
        string selectedLevel)
    {
        _ = backendLessonHistoryClient;
        _ = backendBaseUrl;

        var localItems = lessonHistoryService.LoadCompletedLessons(selectedLevel);
        ReplaceItems(localItems);
        return Task.CompletedTask;
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
