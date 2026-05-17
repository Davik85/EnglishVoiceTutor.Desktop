using System.Diagnostics;
using System.Windows.Controls;
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
        var isPlainEnter = IsEnterKey(e) && !HasShiftModifier();
        if (!isPlainEnter)
        {
            return;
        }

        if (DataContext is not LessonChatViewModel viewModel)
        {
            Debug.WriteLine("Lesson input Enter ignored: DataContext is not LessonChatViewModel.");
            return;
        }

        var canSend = viewModel.SendMessageCommand.CanExecute(null);
        Debug.WriteLine($"Lesson input Enter received: Key={e.Key}; SystemKey={e.SystemKey}; CanExecute={canSend}.");
        if (!canSend)
        {
            return;
        }

        e.Handled = true;
        viewModel.SendMessageCommand.Execute(null);
        Debug.WriteLine("Lesson input Enter executed SendMessageCommand once.");
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
