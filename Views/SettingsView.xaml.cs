using System;
using System.Windows.Controls;
using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class SettingsView : UserControl
{
#if DEBUG
    public const bool ShowDiagnosticsTab = true;
#else
    public const bool ShowDiagnosticsTab = false;
#endif

    private const string LearningTabHeaderText = "Learning";
    private const string AccountTabHeaderText = "Account";
    private const string AudioTabHeaderText = "Audio";
    private const string ProgressTabHeaderText = "Progress";
    private const string DiagnosticsTabHeaderText = "Diagnostics";

    public bool IsDiagnosticsTabVisible => ShowDiagnosticsTab;

    public string LearningTabHeader => LearningTabHeaderText;

    public string AccountTabHeader => AccountTabHeaderText;

    public string AudioTabHeader => AudioTabHeaderText;

    public string ProgressTabHeader => ProgressTabHeaderText;

    public string DiagnosticsTabHeader => DiagnosticsTabHeaderText;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SettingsViewModel oldViewModel)
        {
            oldViewModel.ClearPasswordRequested -= OnClearPasswordRequested;
        }

        if (e.NewValue is SettingsViewModel newViewModel)
        {
            newViewModel.ClearPasswordRequested += OnClearPasswordRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.ClearPasswordRequested -= OnClearPasswordRequested;
        }

        DataContextChanged -= OnDataContextChanged;
        Unloaded -= OnUnloaded;
    }

    private void OnClearPasswordRequested(object? sender, EventArgs e)
    {
        AccountPasswordBox.Clear();
    }

    private void AccountPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel || sender is not PasswordBox passwordBox)
        {
            return;
        }

        viewModel.Password = passwordBox.Password;
    }
}
