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
        if (!IsPlainEnterKey(e))
        {
            return;
        }

        if (DataContext is not LessonChatViewModel viewModel || !viewModel.SendMessageCommand.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        viewModel.SendMessageCommand.Execute(null);
    }

    private static bool IsPlainEnterKey(KeyEventArgs e)
    {
        return e.Key == Key.Return && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.None;
    }
}
