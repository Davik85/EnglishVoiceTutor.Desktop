#!/usr/bin/env python3
"""Regression checks separating technical throttling from product access limits."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DESKTOP_SERVICE = ROOT / "Services" / "LessonChatBackendService.cs"
DESKTOP_VIEW_MODEL = ROOT / "ViewModels" / "LessonChatViewModel.cs"
APP_CONSTANTS = ROOT / "Constants" / "AppConstants.cs"
BACKEND_UX_TEXT = ROOT / "Localization" / "BackendUxLocalizedText.cs"
BACKEND_UX_LOCALIZATION = ROOT / "Localization" / "BackendUxLocalization.cs"
API_PROGRAM = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Program.cs"
RATE_LIMIT_CONSTANTS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Constants" / "RateLimitingConstants.cs"
RATE_LIMITING = ROOT / "backend" / "EnglishVoiceTutor.Api" / "RateLimiting" / "RateLimitingServiceCollectionExtensions.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, path: Path) -> None:
    if needle not in text:
        raise AssertionError(f"Missing required text in {path.relative_to(ROOT)}: {needle}")


def forbid(text: str, needle: str, path: Path) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden text in {path.relative_to(ROOT)}: {needle}")


def main() -> None:
    desktop_service = read(DESKTOP_SERVICE)
    desktop_view_model = read(DESKTOP_VIEW_MODEL)
    app_constants = read(APP_CONSTANTS)
    backend_ux_text = read(BACKEND_UX_TEXT)
    backend_ux_localization = read(BACKEND_UX_LOCALIZATION)
    api_program = read(API_PROGRAM)
    rate_limit_constants = read(RATE_LIMIT_CONSTANTS)
    rate_limiting = read(RATE_LIMITING)

    obsolete_exception = "FreeLimit" + "Exceeded"
    obsolete_configuration = "Free" + "Limits"

    # Desktop must not special-case HTTP 429 as a subscription or product limit.
    forbid(desktop_service, "HttpStatusCode.TooManyRequests", DESKTOP_SERVICE)
    forbid(desktop_service, obsolete_exception, DESKTOP_SERVICE)
    forbid(desktop_view_model, obsolete_exception, DESKTOP_VIEW_MODEL)
    forbid(app_constants, "FreeLimitMessage", APP_CONSTANTS)

    # Technical throttling has an explicit localized message and shared HTTP mapping.
    require(backend_ux_text, "string BackendRequestThrottled,", BACKEND_UX_TEXT)
    require(
        backend_ux_localization,
        "nameof(BackendUxLocalizedText.BackendRequestThrottled)",
        BACKEND_UX_LOCALIZATION,
    )
    require(
        backend_ux_localization,
        "Too many requests right now. Please wait a moment and try again.",
        BACKEND_UX_LOCALIZATION,
    )
    require(
        desktop_view_model,
        "httpRequestException.StatusCode is HttpStatusCode.TooManyRequests",
        DESKTOP_VIEW_MODEL,
    )
    require(
        desktop_view_model,
        "return BackendUxText.BackendRequestThrottled;",
        DESKTOP_VIEW_MODEL,
    )

    # Audio transcription keeps its existing exception type but maps HTTP 429 to the same UX.
    require(
        desktop_view_model,
        "exception.StatusCode is HttpStatusCode.TooManyRequests",
        DESKTOP_VIEW_MODEL,
    )
    require(
        desktop_view_model,
        "? BackendUxText.BackendRequestThrottled",
        DESKTOP_VIEW_MODEL,
    )

    # Normal HTTP failure handling remains in place for chat, hints, and non-streaming TTS.
    if desktop_service.count("response.EnsureSuccessStatusCode();") < 4:
        raise AssertionError("Desktop backend service must preserve normal HTTP failure handling.")

    # The legacy backend enforcement/configuration is gone from active composition.
    forbid(api_program, obsolete_exception, API_PROGRAM)
    forbid(api_program, obsolete_configuration, API_PROGRAM)

    # ASP.NET technical throttling and its safe JSON contract remain unchanged.
    require(rate_limit_constants, 'public const string ErrorCode = "RateLimitExceeded";', RATE_LIMIT_CONSTANTS)
    require(rate_limiting, "options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;", RATE_LIMITING)
    require(rate_limiting, "error = RateLimitingConstants.ErrorCode", RATE_LIMITING)
    require(rate_limiting, "message,", RATE_LIMITING)
    require(rate_limiting, "retryAfterSeconds", RATE_LIMITING)

    print("Technical rate-limit/Desktop product-limit separation policy checks passed.")


if __name__ == "__main__":
    main()
