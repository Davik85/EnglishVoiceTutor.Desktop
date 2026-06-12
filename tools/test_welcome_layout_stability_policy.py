#!/usr/bin/env python3
"""Policy checks for the Welcome/start screen layout stability."""
from __future__ import annotations

import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"
GENERATED_PATH_PATTERNS = (
    re.compile(r"(^|/)(bin|obj|publish|artifacts|dist|tmp|temp)(/|$)", re.I),
    re.compile(r"\.(msi|exe|zip|7z|nupkg|snupkg|mp4|mov|webm)$", re.I),
)


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


def assert_no_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, flags=re.S):
        raise AssertionError(f"Forbidden {label}: {pattern}")


def tracked_files() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=ROOT,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
    )
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def main() -> None:
    welcome_xaml = read("Views/WelcomeView.xaml")
    welcome_code = read("Views/WelcomeView.xaml.cs")
    backend_constants = read("Constants/BackendConstants.cs")
    endpoint_builder = read("Services/BackendEndpointBuilder.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_code = read("Views/SettingsView.xaml.cs")

    assert_not_contains(welcome_xaml, "ActualWidth", "Welcome ActualWidth layout binding")
    assert_not_contains(welcome_xaml, "ActualHeight", "Welcome ActualHeight layout binding")
    assert_not_contains(welcome_xaml, "OnWelcomeRootSizeChanged", "Welcome root SizeChanged layout recalculation")
    assert_not_contains(welcome_xaml, "<ScrollViewer", "Welcome ScrollViewer around hero layout")
    assert_not_contains(welcome_code, "OnWelcomeRootSizeChanged", "Welcome root SizeChanged handler")
    assert_not_contains(welcome_code, "ApplyAdaptiveWelcomeLayout", "per-size Welcome layout recalculation")
    assert_no_regex(welcome_code, r"\.(Width|Height|Margin)\s*=", "Welcome code-behind layout-critical size mutation")

    assert_contains(welcome_xaml, 'UseLayoutRounding="True"', "Welcome layout rounding")
    assert_contains(welcome_xaml, 'SnapsToDevicePixels="True"', "Welcome pixel snapping")
    assert_contains(welcome_xaml, "WelcomeHeroSurface", "Welcome hero clipping surface")
    assert_contains(welcome_xaml, "WelcomeHeroImage", "Welcome hero image")
    assert_contains(welcome_xaml, 'Stretch="UniformToFill"', "Welcome hero cover/crop fill")
    assert_contains(welcome_xaml, 'ClipToBounds="True"', "Welcome clipping without gray bars")
    assert_contains(welcome_code, "Math.Round(heroSize.Width)", "rounded hero clip width")
    assert_contains(welcome_code, "Math.Round(heroSize.Height)", "rounded hero clip height")

    assert_contains(welcome_xaml, "WelcomeHeaderPanel", "Welcome title overlay")
    assert_contains(welcome_xaml, "WelcomePrimaryActionsPanel", "Welcome action overlay")
    assert_contains(welcome_xaml, "StartLessonCommand", "Welcome Start lesson action")
    assert_contains(welcome_xaml, "OpenSettingsCommand", "Welcome Settings action")
    assert_contains(welcome_xaml, "AccountStatusButtonText", "Welcome signed-in/account action")
    assert_contains(welcome_xaml, 'RowDefinition Height="Auto" MinHeight="96"', "reserved primary action row")

    assert_contains(backend_constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "release backend lock constant")
    assert_contains(endpoint_builder, "return BackendConstants.ProductionBackendBaseUrl;", "release backend lock resolver")
    assert_not_contains(settings_xaml, "BackendBaseUrl", "release Settings backend URL editing")
    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics section")
    assert_contains(settings_code, "DesktopDiagnosticsEnabled = false", "release diagnostics UI disabled")

    generated_files = [
        path for path in tracked_files()
        if any(pattern.search(path) for pattern in GENERATED_PATH_PATTERNS)
    ]
    if generated_files:
        raise AssertionError("Generated artifacts are tracked: " + ", ".join(generated_files[:20]))

    print("Welcome layout stability policy checks passed.")


if __name__ == "__main__":
    main()
