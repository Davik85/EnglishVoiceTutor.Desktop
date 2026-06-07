using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop;

public partial class MainWindow : Window
{
    private LessonChatViewModel? currentLessonChatViewModel;
    private bool shutdownCleanupStarted;
    private bool shutdownCleanupCompleted;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
            ApplyLayoutForViewModel(mainViewModel.CurrentViewModel);
        }
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

        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;
        }

        UnsubscribeFromLessonChatViewModel();

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

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.CurrentViewModel) || sender is not MainViewModel mainViewModel)
        {
            return;
        }

        ApplyLayoutForViewModel(mainViewModel.CurrentViewModel);
    }

    private void ApplyLayoutForViewModel(ViewModelBase viewModel)
    {
        if (viewModel is not LessonChatViewModel lessonChatViewModel)
        {
            UnsubscribeFromLessonChatViewModel();
            return;
        }

        SubscribeToLessonChatViewModel(lessonChatViewModel);
        Dispatcher.BeginInvoke(new Action(() => ApplyLessonWindowSize(lessonChatViewModel.IsConversationModeEnabled)), DispatcherPriority.Loaded);
    }

    private void SubscribeToLessonChatViewModel(LessonChatViewModel lessonChatViewModel)
    {
        if (ReferenceEquals(currentLessonChatViewModel, lessonChatViewModel))
        {
            return;
        }

        UnsubscribeFromLessonChatViewModel();
        currentLessonChatViewModel = lessonChatViewModel;
        currentLessonChatViewModel.PropertyChanged += LessonChatViewModel_PropertyChanged;
    }

    private void UnsubscribeFromLessonChatViewModel()
    {
        if (currentLessonChatViewModel is null)
        {
            return;
        }

        currentLessonChatViewModel.PropertyChanged -= LessonChatViewModel_PropertyChanged;
        currentLessonChatViewModel = null;
    }

    private void LessonChatViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LessonChatViewModel.IsConversationModeEnabled) || sender is not LessonChatViewModel lessonChatViewModel)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() => ApplyLessonWindowSize(lessonChatViewModel.IsConversationModeEnabled)), DispatcherPriority.Loaded);
    }

    private void ApplyLessonWindowSize(bool isConversationModeEnabled)
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        if (isConversationModeEnabled)
        {
            ApplyConversationModeWindowSize();
            return;
        }

        ApplyNormalLessonChatWindowSize();
    }

    private void ApplyNormalLessonChatWindowSize()
    {
        var workingArea = GetCurrentMonitorWorkingAreaInDips();
        var targetWidth = GetLessonTargetSize(DesktopLayoutOptions.LessonChatWindowPreferredWidth, DesktopLayoutOptions.LessonChatWindowMinimumReadableWidth, workingArea.Width);
        var targetHeight = GetLessonTargetSize(DesktopLayoutOptions.LessonChatWindowPreferredHeight, DesktopLayoutOptions.LessonChatWindowMinimumReadableHeight, workingArea.Height);
        var currentWidth = ActualWidth > 0 ? ActualWidth : Width;
        var currentHeight = ActualHeight > 0 ? ActualHeight : Height;
        var newWidth = Math.Max(currentWidth, targetWidth);
        var newHeight = Math.Max(currentHeight, targetHeight);
        var expanded = newWidth > currentWidth || newHeight > currentHeight;

        if (expanded)
        {
            Width = newWidth;
            Height = newHeight;
        }

        KeepWindowInsideWorkingArea(workingArea, centerIfExpanded: expanded);
    }

    private void ApplyConversationModeWindowSize()
    {
        var workingArea = GetCurrentMonitorWorkingAreaInDips();
        var targetWidth = GetLessonTargetSize(DesktopLayoutOptions.ConversationModeWindowPreferredWidth, DesktopLayoutOptions.ConversationModeWindowMinimumReadableWidth, workingArea.Width);
        var targetHeight = GetLessonTargetSize(DesktopLayoutOptions.ConversationModeWindowPreferredHeight, DesktopLayoutOptions.ConversationModeWindowMinimumReadableHeight, workingArea.Height);
        var currentWidth = ActualWidth > 0 ? ActualWidth : Width;
        var currentHeight = ActualHeight > 0 ? ActualHeight : Height;
        var resized = Math.Abs(currentWidth - targetWidth) > 0.5 || Math.Abs(currentHeight - targetHeight) > 0.5;

        if (resized)
        {
            Width = targetWidth;
            Height = targetHeight;
        }

        KeepWindowInsideWorkingArea(workingArea, centerIfExpanded: resized);
    }

    private static double GetLessonTargetSize(double preferredSize, double minimumReadableSize, double availableSize)
    {
        if (availableSize <= 0)
        {
            return preferredSize;
        }

        var size = Math.Min(preferredSize, availableSize);
        return availableSize >= minimumReadableSize ? Math.Max(size, minimumReadableSize) : size;
    }

    private void KeepWindowInsideWorkingArea(Rect workingArea, bool centerIfExpanded)
    {
        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            return;
        }

        if (centerIfExpanded && Width <= workingArea.Width && Height <= workingArea.Height)
        {
            Left = workingArea.Left + ((workingArea.Width - Width) / 2);
            Top = workingArea.Top + ((workingArea.Height - Height) / 2);
            return;
        }

        if (Width <= workingArea.Width)
        {
            Left = Math.Min(Math.Max(Left, workingArea.Left), workingArea.Right - Width);
        }
        else
        {
            Left = workingArea.Left;
        }

        if (Height <= workingArea.Height)
        {
            Top = Math.Min(Math.Max(Top, workingArea.Top), workingArea.Bottom - Height);
        }
        else
        {
            Top = workingArea.Top;
        }
    }

    private Rect GetCurrentMonitorWorkingAreaInDips()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        var monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (monitorHandle != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo
            {
                Size = Marshal.SizeOf<MonitorInfo>()
            };

            if (GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return DevicePixelsToDips(ToRect(monitorInfo.WorkArea));
            }
        }

        return SystemParameters.WorkArea;
    }

    private Rect DevicePixelsToDips(Rect devicePixelRect)
    {
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new Point(devicePixelRect.Left, devicePixelRect.Top));
        var bottomRight = transform.Transform(new Point(devicePixelRect.Right, devicePixelRect.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private static Rect ToRect(NativeRect rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfo monitorInfo);

    private const uint MonitorDefaultToNearest = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
