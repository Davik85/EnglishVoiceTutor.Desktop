using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class LessonChatView : UserControl
{
    private bool areLessonInputEnterHandlersAttached;
    private bool isLessonInputEnterSendInProgress;

    public LessonChatView()
    {
        InitializeComponent();
        Loaded += LessonChatView_Loaded;
        Unloaded += LessonChatView_Unloaded;
    }

    private void LessonChatView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachLessonInputEnterHandlers();
    }

    private void LessonChatView_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LessonChatView_Loaded;
        Unloaded -= LessonChatView_Unloaded;
    }

    private void AttachLessonInputEnterHandlers()
    {
        if (areLessonInputEnterHandlersAttached)
        {
            return;
        }

        LessonInputTextBox.AddHandler(
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(LessonInputTextBox_PreviewKeyDown),
            handledEventsToo: true);
        LessonInputTextBox.AddHandler(
            Keyboard.KeyDownEvent,
            new KeyEventHandler(LessonInputTextBox_KeyDown),
            handledEventsToo: true);
        areLessonInputEnterHandlersAttached = true;
        Debug.WriteLine("Lesson input Enter handlers attached: Target=LessonInputTextBox; Events=PreviewKeyDown,KeyDown; HandledEventsToo=True.");
    }

    private void LessonInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        HandleLessonInputEnterKeyDown(sender, e, "PreviewKeyDown");
    }

    private void LessonInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        HandleLessonInputEnterKeyDown(sender, e, "KeyDown");
    }

    private void HandleLessonInputEnterKeyDown(object sender, KeyEventArgs e, string eventType)
    {
        if (!ReferenceEquals(sender, LessonInputTextBox) || !IsEventFromLessonInput(e.OriginalSource))
        {
            Debug.WriteLine($"Lesson input Enter ignored: EventType={eventType}; SenderType={sender?.GetType().FullName ?? "null"}; OriginalSourceType={e.OriginalSource?.GetType().FullName ?? "null"}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; Reason=not_lesson_input.");
            return;
        }

        var isEnterKey = IsEnterKey(e);
        var hasShiftModifier = HasShiftModifier();
        var textLengthBeforeBindingUpdate = LessonInputTextBox.Text?.Length ?? 0;

        if (!isEnterKey)
        {
            return;
        }

        if (hasShiftModifier)
        {
            Debug.WriteLine($"Lesson input Enter ignored: EventType={eventType}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLengthBeforeBindingUpdate}; Reason=shift_enter.");
            return;
        }

        if (isLessonInputEnterSendInProgress)
        {
            e.Handled = true;
            Debug.WriteLine($"Lesson input Enter ignored: EventType={eventType}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLengthBeforeBindingUpdate}; Reason=duplicate_key_event_guard; Executed=False.");
            return;
        }

        if (DataContext is not LessonChatViewModel viewModel)
        {
            Debug.WriteLine($"Lesson input Enter ignored: EventType={eventType}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLengthBeforeBindingUpdate}; DataContext={DataContext?.GetType().FullName ?? "null"}; Reason=invalid_data_context; Executed=False.");
            return;
        }

        BindingExpression? binding = LessonInputTextBox.GetBindingExpression(TextBox.TextProperty);
        binding?.UpdateSource();

        var textLength = LessonInputTextBox.Text?.Length ?? 0;
        var canExecute = viewModel.SendMessageCommand.CanExecute(null);
        var willExecute = viewModel.CanTypeText && textLength > 0 && canExecute;
        Debug.WriteLine($"Lesson input Enter received: EventType={eventType}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; CanTypeText={viewModel.CanTypeText}; SendCanExecute={canExecute}; Executed={willExecute}.");

        if (!willExecute)
        {
            return;
        }

        isLessonInputEnterSendInProgress = true;
        e.Handled = true;
        viewModel.SendMessageCommand.Execute(null);
        Debug.WriteLine($"Lesson input Enter executed SendMessageCommand once: EventType={eventType}; Key={e.Key}; SystemKey={e.SystemKey}; ImeProcessedKey={e.ImeProcessedKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; CanTypeText={viewModel.CanTypeText}; SendCanExecute={canExecute}; Executed=True.");
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => isLessonInputEnterSendInProgress = false));
    }

    private bool IsEventFromLessonInput(object originalSource)
    {
        if (ReferenceEquals(originalSource, LessonInputTextBox))
        {
            return true;
        }

        if (originalSource is not DependencyObject sourceDependencyObject)
        {
            return false;
        }

        DependencyObject? dependencyObject = sourceDependencyObject;
        while (dependencyObject is not null)
        {
            if (ReferenceEquals(dependencyObject, LessonInputTextBox))
            {
                return true;
            }

            dependencyObject = GetParentObject(dependencyObject);
        }

        return false;
    }

    private static DependencyObject? GetParentObject(DependencyObject dependencyObject)
    {
        if (dependencyObject is Visual or Visual3D)
        {
            return VisualTreeHelper.GetParent(dependencyObject);
        }

        return LogicalTreeHelper.GetParent(dependencyObject);
    }

    private static bool IsEnterKey(KeyEventArgs e)
    {
        return e.Key == Key.Return
            || e.Key == Key.Enter
            || e.SystemKey == Key.Return
            || e.SystemKey == Key.Enter
            || e.ImeProcessedKey == Key.Return
            || e.ImeProcessedKey == Key.Enter;
    }

    private static bool HasShiftModifier()
    {
        return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
    }
}
