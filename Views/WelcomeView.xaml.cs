using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private void OnWelcomeHeroSurfaceSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyWelcomeHeroClip(e.NewSize);
    }

    private void ApplyWelcomeHeroClip(Size heroSize)
    {
        if (heroSize.Width <= 0 || heroSize.Height <= 0)
        {
            return;
        }

        var roundedWidth = Math.Round(heroSize.Width);
        var roundedHeight = Math.Round(heroSize.Height);
        var cornerRadius = Math.Round(WelcomeCard.CornerRadius.TopLeft);
        WelcomeHeroSurface.Clip = new RectangleGeometry(
            new Rect(0, 0, roundedWidth, roundedHeight),
            cornerRadius,
            cornerRadius);
    }
}
