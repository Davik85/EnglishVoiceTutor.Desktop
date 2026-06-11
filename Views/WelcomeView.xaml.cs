using System.Windows;
using System.Windows.Controls;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private void OnWelcomeRootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyAdaptiveWelcomeLayout(e.NewSize);
    }

    private void ApplyAdaptiveWelcomeLayout(Size availableSize)
    {
        var isCompactHeight = availableSize.Height <= DesktopLayoutOptions.WelcomeCompactHeightThreshold;
        var outerMargin = isCompactHeight
            ? DesktopLayoutOptions.WelcomeCompactOuterMargin
            : DesktopLayoutOptions.WelcomeNormalOuterMargin;
        var overlayHorizontalMargin = isCompactHeight
            ? DesktopLayoutOptions.WelcomeCompactOverlayHorizontalMargin
            : DesktopLayoutOptions.WelcomeNormalOverlayHorizontalMargin;
        var overlayTopMargin = isCompactHeight
            ? DesktopLayoutOptions.WelcomeCompactOverlayTopMargin
            : DesktopLayoutOptions.WelcomeNormalOverlayTopMargin;
        var overlayBottomMargin = isCompactHeight
            ? DesktopLayoutOptions.WelcomeCompactOverlayBottomMargin
            : DesktopLayoutOptions.WelcomeNormalOverlayBottomMargin;

        WelcomeRoot.Margin = new Thickness(outerMargin);
        WelcomeOverlay.Margin = new Thickness(overlayHorizontalMargin, overlayTopMargin, overlayHorizontalMargin, overlayBottomMargin);

        var reservedOuterHeight = outerMargin * 2;
        var reservedOuterWidth = outerMargin * 2;
        var availableCardHeight = Math.Max(
            DesktopLayoutOptions.WelcomeCardMinimumHeight,
            availableSize.Height - reservedOuterHeight);
        var availableCardWidth = Math.Max(
            DesktopLayoutOptions.WelcomePanelMaximumWidth,
            availableSize.Width - reservedOuterWidth);
        var targetCardHeight = Math.Min(DesktopLayoutOptions.WelcomeCardMaximumHeight, availableCardHeight);
        var targetCardWidth = Math.Min(DesktopLayoutOptions.WelcomeCardMaximumWidth, availableCardWidth);
        var targetPanelWidth = Math.Min(
            DesktopLayoutOptions.WelcomePanelMaximumWidth,
            targetCardWidth - (overlayHorizontalMargin * 2));

        WelcomeCard.Height = targetCardHeight;
        WelcomeCard.Width = targetCardWidth;
        WelcomeHeaderPanel.Width = targetPanelWidth;
        WelcomePrimaryActionsPanel.Width = targetPanelWidth;
        WelcomeHeroImage.MaxHeight = Math.Max(
            DesktopLayoutOptions.WelcomeHeroMinimumHeight,
            targetCardHeight - DesktopLayoutOptions.WelcomeMinimumActionAreaHeight);
    }
}
