using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class LessonChatView : UserControl
{
    public LessonChatView()
    {
        InitializeComponent();
    }

    private void LessonInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox inputTextBox)
        {
            Debug.WriteLine($"Lesson input Enter ignored: SenderType={sender?.GetType().FullName ?? "null"}; Key={e.Key}; SystemKey={e.SystemKey}; Modifiers={Keyboard.Modifiers}.");
            return;
        }

        var isEnterKey = IsEnterKey(e);
        var hasShiftModifier = HasShiftModifier();
        var textLength = inputTextBox.Text?.Length ?? 0;

        if (!isEnterKey || hasShiftModifier)
        {
            if (isEnterKey)
            {
                Debug.WriteLine($"Lesson input Enter ignored: Key={e.Key}; SystemKey={e.SystemKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; Reason=shift_enter_or_modified_enter.");
            }

            return;
        }

        if (DataContext is not LessonChatViewModel viewModel)
        {
            Debug.WriteLine($"Lesson input Enter ignored: Key={e.Key}; SystemKey={e.SystemKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; DataContext={DataContext?.GetType().FullName ?? "null"}; Reason=invalid_data_context.");
            return;
        }

        BindingExpression? binding = inputTextBox.GetBindingExpression(TextBox.TextProperty);
        binding?.UpdateSource();

        var canExecute = viewModel.SendMessageCommand.CanExecute(null);
        var willExecute = viewModel.CanTypeText && textLength > 0 && canExecute;
        Debug.WriteLine($"Lesson input Enter received: Key={e.Key}; SystemKey={e.SystemKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; CanTypeText={viewModel.CanTypeText}; SendCanExecute={canExecute}; WillExecute={willExecute}.");

        if (!willExecute)
        {
            return;
        }

        e.Handled = true;
        viewModel.SendMessageCommand.Execute(null);
        Debug.WriteLine($"Lesson input Enter executed SendMessageCommand once: Key={e.Key}; SystemKey={e.SystemKey}; Modifiers={Keyboard.Modifiers}; TextLength={textLength}; CanTypeText={viewModel.CanTypeText}; SendCanExecute={canExecute}; Executed=True.");
    }

    private static bool IsEnterKey(KeyEventArgs e)
    {
        return e.Key == Key.Return
            || e.Key == Key.Enter
            || e.SystemKey == Key.Return
            || e.SystemKey == Key.Enter;
    }

    private static bool HasShiftModifier()
    {
        return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
    }
}
