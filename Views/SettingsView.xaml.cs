using System;
using System.Windows.Controls;
using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class SettingsView : UserControl
{
    private const string ReleaseDiagnosticsSupportFlagName = "EVT_DESKTOP_DIAGNOSTICS";
#if DEBUG
    public static readonly bool DesktopDiagnosticsEnabled = true;
#else
    public static readonly bool DesktopDiagnosticsEnabled = IsReleaseDiagnosticsSupportFlagEnabled();
#endif

    private const string LearningTabHeaderText = "Learning";
    private const string AccountTabHeaderText = "Account";
    private const string AudioTabHeaderText = "Audio";
    private const string ProgressTabHeaderText = "Progress";
    private const string DiagnosticsTabHeaderText = "Diagnostics";

    public bool IsDiagnosticsTabVisible => DesktopDiagnosticsEnabled;

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

    private static bool IsReleaseDiagnosticsSupportFlagEnabled()
    {
        var flagValue = Environment.GetEnvironmentVariable(ReleaseDiagnosticsSupportFlagName);

        return string.Equals(flagValue, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flagValue, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flagValue, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SettingsViewModel oldViewModel)
        {
            oldViewModel.ClearPasswordRequested -= OnClearPasswordRequested;
            oldViewModel.ClearPasswordRecoveryFieldsRequested -= OnClearPasswordRecoveryFieldsRequested;
        }

        if (e.NewValue is SettingsViewModel newViewModel)
        {
            newViewModel.ClearPasswordRequested += OnClearPasswordRequested;
            newViewModel.ClearPasswordRecoveryFieldsRequested += OnClearPasswordRecoveryFieldsRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.ClearPasswordRequested -= OnClearPasswordRequested;
            viewModel.ClearPasswordRecoveryFieldsRequested -= OnClearPasswordRecoveryFieldsRequested;
        }

        DataContextChanged -= OnDataContextChanged;
        Unloaded -= OnUnloaded;
    }

    private void OnClearPasswordRequested(object? sender, EventArgs e)
    {
        AccountPasswordBox.Clear();
    }

    private void OnClearPasswordRecoveryFieldsRequested(object? sender, EventArgs e)
    {
        ResetNewPasswordBox.Clear();
        ResetConfirmPasswordBox.Clear();
        CurrentPasswordBox.Clear();
        ChangeNewPasswordBox.Clear();
        ChangeConfirmPasswordBox.Clear();
    }

    private void AccountPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel || sender is not PasswordBox passwordBox)
        {
            return;
        }

        viewModel.Password = passwordBox.Password;
    }

    private void ResetNewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ResetNewPassword = passwordBox.Password;
        }
    }

    private void ResetConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ResetConfirmPassword = passwordBox.Password;
        }
    }

    private void CurrentPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.CurrentPassword = passwordBox.Password;
        }
    }

    private void ChangeNewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ChangeNewPassword = passwordBox.Password;
        }
    }

    private void ChangeConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ChangeConfirmPassword = passwordBox.Password;
        }
    }
}
