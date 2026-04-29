using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnglishVoiceTutor.Desktop.Behaviors;

public static class ScrollViewerMouseWheelBehavior
{
    public static readonly DependencyProperty BubbleMouseWheelWhenAtEdgeProperty =
        DependencyProperty.RegisterAttached(
            "BubbleMouseWheelWhenAtEdge",
            typeof(bool),
            typeof(ScrollViewerMouseWheelBehavior),
            new PropertyMetadata(false, OnBubbleMouseWheelWhenAtEdgeChanged));

    public static bool GetBubbleMouseWheelWhenAtEdge(DependencyObject obj)
    {
        return (bool)obj.GetValue(BubbleMouseWheelWhenAtEdgeProperty);
    }

    public static void SetBubbleMouseWheelWhenAtEdge(DependencyObject obj, bool value)
    {
        obj.SetValue(BubbleMouseWheelWhenAtEdgeProperty, value);
    }

    private static void OnBubbleMouseWheelWhenAtEdgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            return;
        }

        scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (!ShouldBubble(scrollViewer, e.Delta))
        {
            return;
        }

        e.Handled = true;

        var parent = FindParentUiElement(scrollViewer);
        if (parent is null)
        {
            return;
        }

        var bubbleEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = scrollViewer
        };

        parent.RaiseEvent(bubbleEvent);
    }

    private static bool ShouldBubble(ScrollViewer scrollViewer, int delta)
    {
        if (scrollViewer.ScrollableHeight <= 0)
        {
            return true;
        }

        if (delta > 0 && scrollViewer.VerticalOffset <= 0)
        {
            return true;
        }

        if (delta < 0 && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
        {
            return true;
        }

        return false;
    }

    private static UIElement? FindParentUiElement(DependencyObject current)
    {
        var parent = VisualTreeHelper.GetParent(current);

        while (parent is not null)
        {
            if (parent is UIElement element)
            {
                return element;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
}
