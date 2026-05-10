using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EnglishVoiceTutor.Desktop.Behaviors;

public static class ScrollViewerAutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollToBottomProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToBottom",
            typeof(bool),
            typeof(ScrollViewerAutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollToBottomChanged));

    public static bool GetAutoScrollToBottom(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToBottomProperty);
    }

    public static void SetAutoScrollToBottom(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToBottomProperty, value);
    }

    private static void OnAutoScrollToBottomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.ScrollChanged -= OnScrollChanged;

        if ((bool)e.NewValue)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || e.ExtentHeightChange <= 0)
        {
            return;
        }

        _ = scrollViewer.Dispatcher.BeginInvoke(
            new Action(scrollViewer.ScrollToBottom),
            DispatcherPriority.Background);
    }
}
