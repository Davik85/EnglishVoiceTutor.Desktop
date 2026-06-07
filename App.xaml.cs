using System;
using System.Windows;
using Velopack;

namespace EnglishVoiceTutor.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run(new MainWindow());
        }
    }
}
