using System.Windows;
using EnglishVoiceTutor.Desktop.ViewModels;

namespace EnglishVoiceTutor.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
