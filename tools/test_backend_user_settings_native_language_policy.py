#!/usr/bin/env python3
"""Policy checks for native-language persistence in backend and desktop settings sync."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKEND_REQUEST = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/UserSettings/UpdateUserSettingsRequest.cs"
BACKEND_RESPONSE = ROOT / "backend/EnglishVoiceTutor.Api/Contracts/UserSettings/UserSettingsResponse.cs"
BACKEND_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/UserSettingsService.cs"
DESKTOP_REQUEST = ROOT / "Models/UpdateBackendUserSettingsRequest.cs"
DESKTOP_RESPONSE = ROOT / "Models/BackendUserSettingsResponse.cs"
SETTINGS_VM = ROOT / "ViewModels/SettingsViewModel.cs"


def read(path: Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    backend_request = read(BACKEND_REQUEST)
    backend_response = read(BACKEND_RESPONSE)
    backend_service = read(BACKEND_SERVICE)
    desktop_request = read(DESKTOP_REQUEST)
    desktop_response = read(DESKTOP_RESPONSE)
    settings_vm = read(SETTINGS_VM)

    assert_contains(backend_request, "public string NativeLanguage", "backend update request NativeLanguage")
    assert_contains(backend_response, "string NativeLanguage", "backend settings response NativeLanguage")
    assert_contains(desktop_request, "public string NativeLanguage", "desktop update request NativeLanguage")
    assert_contains(desktop_response, "public string NativeLanguage", "desktop settings response NativeLanguage")

    for snippet in [
        "profile.NativeLanguage = NativeLanguageCatalog.GetByIdOrName(request.NativeLanguage).Id;",
        "profile.UpdatedAt = now;",
        "!string.IsNullOrWhiteSpace(request.NativeLanguage) && !NativeLanguageCatalog.IsSupported(request.NativeLanguage)",
        "settings.ExplanationLanguage = NativeLanguageCatalog.GetByIdOrName(request.ExplanationLanguage).Id;",
        "settings.StudyLanguage = StudyLanguageConstants.ToCanonicalValue(request.StudyLanguage);",
    ]:
        assert_contains(backend_service, snippet, "backend native-language persistence")

    for snippet in [
        "NativeLanguage = SelectedNativeLanguageOption.Id,",
        "ExplanationLanguage = SelectedInterfaceLanguageOption.Id,",
        "SelectedNativeLanguageOption = NativeLanguageCatalog.GetByIdOrName(settings.NativeLanguage);",
    ]:
        assert_contains(settings_vm, snippet, "desktop native/explanation-language sync")

    assert_not_contains(settings_vm, "ExplanationLanguage = SelectedNativeLanguageOption.Id", "native language sent as explanation language")

    print("Backend user settings native-language policy checks passed.")


if __name__ == "__main__":
    main()
