using System;
using System.Windows.Controls;
using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class SettingsView : UserControl
{
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
