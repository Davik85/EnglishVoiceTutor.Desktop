using System.Windows;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services.Windowing;

public static class WindowPlacementService
{
    public static Size GetSafeMinimumSize(Rect workingArea)
    {
        return new Size(
            ClampMinimumToWorkingArea(DesktopLayoutOptions.MinimumWindowWidth, workingArea.Width),
            ClampMinimumToWorkingArea(DesktopLayoutOptions.MinimumWindowHeight, workingArea.Height));
    }

    public static Size GetSafeStartupSize(Rect workingArea)
    {
        return GetSafeSize(
            DesktopLayoutOptions.StartWindowWidth,
            DesktopLayoutOptions.StartWindowHeight,
            workingArea,
            DesktopLayoutOptions.StartupWidthWorkingAreaRatio,
            DesktopLayoutOptions.StartupHeightWorkingAreaRatio,
            GetSafeMinimumSize(workingArea));
    }

    public static Size GetSafeLessonSize(
        double preferredWidth,
        double preferredHeight,
        double minimumReadableWidth,
        double minimumReadableHeight,
        Rect workingArea)
    {
        var safeMinimumSize = new Size(
            ClampMinimumToWorkingArea(minimumReadableWidth, workingArea.Width),
            ClampMinimumToWorkingArea(minimumReadableHeight, workingArea.Height));

        return GetSafeSize(
            preferredWidth,
            preferredHeight,
            workingArea,
            DesktopLayoutOptions.LessonWidthWorkingAreaRatio,
            DesktopLayoutOptions.LessonHeightWorkingAreaRatio,
            safeMinimumSize);
    }

    public static Point GetCenteredPosition(Size windowSize, Rect workingArea)
    {
        return new Point(
            workingArea.Left + ((workingArea.Width - windowSize.Width) / 2),
            workingArea.Top + ((workingArea.Height - windowSize.Height) / 2));
    }

    public static Point ClampPosition(Point requestedPosition, Size windowSize, Rect workingArea)
    {
        var left = ClampCoordinate(requestedPosition.X, workingArea.Left, workingArea.Right - windowSize.Width);
        var top = ClampCoordinate(requestedPosition.Y, workingArea.Top, workingArea.Bottom - windowSize.Height);

        return new Point(left, top);
    }

    private static Size GetSafeSize(
        double preferredWidth,
        double preferredHeight,
        Rect workingArea,
        double widthWorkingAreaRatio,
        double heightWorkingAreaRatio,
        Size safeMinimumSize)
    {
        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            return new Size(preferredWidth, preferredHeight);
        }

        var maximumWidth = Math.Max(DesktopLayoutOptions.MinimumUsableWindowWidth, workingArea.Width * widthWorkingAreaRatio);
        var maximumHeight = Math.Max(DesktopLayoutOptions.MinimumUsableWindowHeight, workingArea.Height * heightWorkingAreaRatio);
        maximumWidth = Math.Min(maximumWidth, workingArea.Width);
        maximumHeight = Math.Min(maximumHeight, workingArea.Height);

        return new Size(
            ClampSize(preferredWidth, safeMinimumSize.Width, maximumWidth),
            ClampSize(preferredHeight, safeMinimumSize.Height, maximumHeight));
    }

    private static double ClampMinimumToWorkingArea(double requestedMinimum, double availableSize)
    {
        if (availableSize <= 0)
        {
            return requestedMinimum;
        }

        return Math.Min(requestedMinimum, availableSize);
    }

    private static double ClampSize(double preferredSize, double minimumSize, double maximumSize)
    {
        var safeMaximumSize = Math.Max(0, maximumSize);
        var safeMinimumSize = Math.Min(Math.Max(0, minimumSize), safeMaximumSize);
        return Math.Min(Math.Max(preferredSize, safeMinimumSize), safeMaximumSize);
    }

    private static double ClampCoordinate(double requestedCoordinate, double minimumCoordinate, double maximumCoordinate)
    {
        var safeMaximumCoordinate = Math.Max(minimumCoordinate, maximumCoordinate);
        return Math.Min(Math.Max(requestedCoordinate, minimumCoordinate), safeMaximumCoordinate);
    }
}
