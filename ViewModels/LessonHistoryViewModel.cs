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

    public LessonHistoryViewModel(AppLocalizedText localizedText, LessonHistoryService lessonHistoryService, Action navigateBack)
    {
        this.localizedText = localizedText;
        this.navigateBack = navigateBack;
        Items = new ObservableCollection<LessonHistoryItem>(lessonHistoryService.Load());
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
