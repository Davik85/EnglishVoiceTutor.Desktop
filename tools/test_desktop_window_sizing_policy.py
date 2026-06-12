#!/usr/bin/env python3
"""Policy checks for safe WPF desktop startup sizing and placement."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.exists():
        raise AssertionError(f"Missing required file: {relative}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def read_constant(constants: str, name: str) -> float:
    match = re.search(rf"public const double {re.escape(name)} = ([0-9.]+);", constants)
    if match is None:
        raise AssertionError(f"Missing named size constant: {name}")
    return float(match.group(1))


def main() -> None:
    main_xaml = read("MainWindow.xaml")
    main_code = read("MainWindow.xaml.cs")
    layout_constants = read("Models/DesktopLayoutOptions.cs")
    placement_service = read("Services/Windowing/WindowPlacementService.cs")
    welcome_xaml = read("Views/WelcomeView.xaml")
    welcome_code = read("Views/WelcomeView.xaml.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    backend_constants = read("Constants/BackendConstants.cs")
    endpoint_builder = read("Services/BackendEndpointBuilder.cs")
    docs = "\n".join(read(path) for path in [
        "docs/CURRENT_STATE.md",
        "docs/NEXT_STEPS.md",
        "docs/TESTER_RELEASE.md",
    ])

    assert_contains(main_xaml, 'WindowStartupLocation="CenterScreen"', "center-screen startup mode")
    assert_contains(main_code, "ApplySafeStartupWindowPlacement(SystemParameters.WorkArea)", "pre-show safe primary startup sizing")
    assert_contains(main_code, "OnSourceInitialized", "source-initialized monitor validation")
    assert_contains(main_code, "GetCurrentMonitorWorkingAreaInDips", "current monitor working-area lookup")
    assert_contains(main_code, "MonitorFromWindow", "monitor-aware working-area selection")
    assert_contains(main_code, "GetMonitorInfo", "monitor working area query")
    assert_contains(main_code, "DevicePixelsToDips", "DPI-aware monitor coordinate conversion")
    assert_contains(placement_service, "GetSafeStartupSize", "safe startup size helper")
    assert_contains(placement_service, "GetSafeMinimumSize", "safe dynamic minimum helper")
    assert_contains(placement_service, "GetCenteredPosition", "safe centered startup position helper")
    assert_contains(placement_service, "ClampPosition", "safe position clamp helper")
    assert_contains(placement_service, "workingArea.Right - windowSize.Width", "right edge clamp")
    assert_contains(placement_service, "workingArea.Bottom - windowSize.Height", "bottom edge clamp")
    assert_contains(main_code, "Top = clampedPosition.Y", "top edge is clamped before use")
    assert_contains(main_code, "Left = clampedPosition.X", "left edge is clamped before use")
    assert_contains(main_code, "WindowState != WindowState.Normal", "app is not maximized by default")
    assert_not_contains(main_xaml, "WindowState=\"Maximized\"", "default maximized window")
    assert_not_contains(main_xaml, "ResizeMode=\"NoResize\"", "resize lock")

    startup_width_ratio = read_constant(layout_constants, "StartupWidthWorkingAreaRatio")
    startup_height_ratio = read_constant(layout_constants, "StartupHeightWorkingAreaRatio")
    minimum_width = read_constant(layout_constants, "MinimumWindowWidth")
    minimum_height = read_constant(layout_constants, "MinimumWindowHeight")
    start_width = read_constant(layout_constants, "StartWindowWidth")
    start_height = read_constant(layout_constants, "StartWindowHeight")

    if not 0.92 <= startup_width_ratio <= 0.96:
        raise AssertionError("Startup width ratio must stay in the safe 92-96% range.")
    if not 0.88 <= startup_height_ratio <= 0.94:
        raise AssertionError("Startup height ratio must stay in the safe 88-94% range.")
    if minimum_width > 1366:
        raise AssertionError("Minimum width cannot exceed a 1366px laptop screen.")
    if minimum_height > 720 * startup_height_ratio:
        raise AssertionError("Minimum height cannot force the window outside a 1280x720 scaled laptop working area.")
    if start_width < minimum_width or start_height < minimum_height:
        raise AssertionError("Default startup size must not be smaller than the named minimum size.")

    assert_contains(placement_service, "DesktopLayoutOptions.StartupWidthWorkingAreaRatio", "named startup width ratio")
    assert_contains(placement_service, "DesktopLayoutOptions.StartupHeightWorkingAreaRatio", "named startup height ratio")
    assert_contains(placement_service, "DesktopLayoutOptions.MinimumUsableWindowWidth", "named emergency minimum width")
    assert_contains(placement_service, "DesktopLayoutOptions.MinimumUsableWindowHeight", "named emergency minimum height")
    assert_contains(main_code, "ApplySafeMinimumSize(workingArea)", "dynamic minimum cannot exceed working area")
    assert_contains(main_code, "Math.Min(Math.Max(Width, MinWidth), workingArea.Width)", "width cannot exceed working area")
    assert_contains(main_code, "Math.Min(Math.Max(Height, MinHeight), workingArea.Height)", "height cannot exceed working area")

    assert_not_contains(welcome_xaml, "<ScrollViewer", "welcome root ScrollViewer hiding primary actions below the fold")
    assert_contains(welcome_xaml, "WelcomePrimaryActionsPanel", "named welcome primary action area")
    assert_contains(welcome_xaml, "WelcomeHeroImage", "named welcome hero image")
    assert_contains(welcome_xaml, 'Stretch="UniformToFill"', "welcome hero cover-style fill/crop behavior")
    assert_not_contains(welcome_xaml, 'Stretch="Uniform"', "welcome hero letterbox image stretch")
    assert_not_contains(welcome_xaml, 'MaxHeight="430"', "welcome hero image max-height letterboxing")
    assert_contains(welcome_xaml, "WelcomeHeroSurface", "named welcome hero clipping surface")
    assert_contains(welcome_xaml, 'ClipToBounds="True"', "welcome hero/card clipping")
    assert_contains(welcome_code, "RectangleGeometry", "welcome rounded-corner clip geometry")
    assert_contains(welcome_code, "Math.Round(heroSize.Width)", "welcome hero clip rounds width to stable pixels")
    assert_contains(welcome_code, "Math.Round(heroSize.Height)", "welcome hero clip rounds height to stable pixels")
    assert_not_contains(welcome_code, "WelcomeHeroImage.Width =", "welcome code-behind hero width feedback loop")
    assert_not_contains(welcome_code, "WelcomeHeroImage.Height =", "welcome code-behind hero height feedback loop")
    assert_contains(welcome_xaml, 'RowDefinition Height="Auto" MinHeight="96"', "welcome primary action row reserves visible space")
    assert_contains(layout_constants, "WelcomeCompactHeightThreshold", "named compact welcome threshold")
    assert_contains(layout_constants, "WelcomeCardMaximumHeight", "named welcome card max height")
    assert_contains(layout_constants, "WelcomeMinimumActionAreaHeight", "named welcome action area reserve")
    assert_not_contains(welcome_code, "ApplyAdaptiveWelcomeLayout", "welcome per-size layout feedback loop")
    assert_not_contains(welcome_xaml, "OnWelcomeRootSizeChanged", "welcome root SizeChanged layout recalculation")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Settings backend URL field")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings backend URL label")
    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_contains(backend_constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend URL")
    assert_contains(endpoint_builder, "return BackendConstants.ProductionBackendBaseUrl;", "release backend remains server-only")

    combined_desktop_code = "\n".join(read(path) for path in [
        "MainWindow.xaml.cs",
        "ViewModels/MainViewModel.cs",
        "ViewModels/SettingsViewModel.cs",
        "Services/Auth/AuthBackendService.cs",
        "Services/LessonChatBackendService.cs",
        "Services/BackendLessonHistoryClient.cs",
        "Services/BackendLessonSummaryClient.cs",
        "Services/BackendEndpointBuilder.cs",
    ])
    for forbidden in [".Result", ".Wait(", "GetAwaiter().GetResult()"]:
        assert_not_contains(combined_desktop_code, forbidden, "blocking async pattern")

    assert_contains(docs, "clamps startup size and position to the visible working area", "window sizing docs")
    assert_contains(docs, "smaller laptop / scaled display", "scaled display smoke docs")
    assert_contains(docs, "Welcome/start screen primary actions are visible", "welcome primary action visibility docs")
    assert_contains(docs, "Backend/auth/lessons remain unchanged", "unchanged backend/auth/lessons docs")

    print("Desktop window sizing policy checks passed.")


if __name__ == "__main__":
    main()
