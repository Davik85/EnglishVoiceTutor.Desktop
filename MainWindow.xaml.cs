using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
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

    protected override void OnClosing(CancelEventArgs e)
    {
        if (shutdownCleanupCompleted)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;

        if (shutdownCleanupStarted)
        {
            return;
        }

        shutdownCleanupStarted = true;
        _ = RunShutdownCleanupAndCloseAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        shutdownCleanupCompleted = true;

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private async Task RunShutdownCleanupAndCloseAsync()
    {
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
            await Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.ApplicationIdle);
        }
    }
}
