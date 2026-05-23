using System.Windows.Controls;
using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
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
