using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop;

public partial class MainWindow : Window
{
    private bool shutdownCleanupStarted;
    private bool shutdownCleanupCompleted;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (!shutdownCleanupCompleted && !shutdownCleanupStarted)
        {
            e.Cancel = true;
            shutdownCleanupStarted = true;

            try
            {
                if (DataContext is MainViewModel mainViewModel)
                {
                    await mainViewModel.ShutdownAsync();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Desktop shutdown cleanup failed without blocking window close: {exception.Message}");
            }
            finally
            {
                shutdownCleanupCompleted = true;
                Close();
            }

            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
