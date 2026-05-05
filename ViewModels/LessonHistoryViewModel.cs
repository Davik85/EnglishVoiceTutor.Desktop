using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services;

namespace EnglishVoiceTutor.Desktop.ViewModels;

public partial class LessonHistoryViewModel : ViewModelBase
{
    private readonly Action navigateBack;

    public string Title => AppConstants.LessonHistoryTitle;

    public string Subtitle => AppConstants.LessonHistorySubtitle;

    public string EmptyHistoryText => AppConstants.EmptyLessonHistoryText;

    public string BackButtonText => AppConstants.LessonHistoryBackButtonText;

    public string GoodLabel => AppConstants.LessonHistoryGoodLabel;

    public string ImproveLabel => AppConstants.LessonHistoryImproveLabel;

    public ObservableCollection<LessonHistoryItem> Items { get; }

    public bool HasHistory => Items.Count > 0;

    public LessonHistoryViewModel(LessonHistoryService lessonHistoryService, Action navigateBack)
    {
        this.navigateBack = navigateBack;
        Items = new ObservableCollection<LessonHistoryItem>(lessonHistoryService.Load());
    }

    [RelayCommand]
    private void Back()
    {
        navigateBack();
    }
}
