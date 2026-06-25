#!/usr/bin/env python3
"""Policy checks for aggregate-only admin product statistics safety."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminProductStatisticsService.cs"
DEVICE_SERVICE = ROOT / "backend/EnglishVoiceTutor.Api/Services/Devices/DeviceRegistrationService.cs"
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
ADMIN_HTML = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"


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
    service = read(SERVICE)
    device_service = read(DEVICE_SERVICE)
    admin_js = read(ADMIN_JS)
    admin_html = read(ADMIN_HTML)


    for snippet in [
        "&& item.Platform == platform\n                && item.DeviceName == deviceName, cancellationToken)",
        "device.LastSeenAt = now;\n            device.AppVersion = appVersion;",
        "AppVersion = appVersion",
    ]:
        assert_contains(device_service, snippet, "privacy-safe tracked device upsert identity")

    assert_not_contains(
        device_service,
        "&& item.DeviceName == deviceName\n                && item.AppVersion == appVersion",
        "app version in tracked device lookup identity",
    )

    for snippet in [
        ".AsNoTracking()\n            .Select(settings => settings.StudyLanguage)\n            .ToListAsync(cancellationToken)",
        ".Select(session => new { session.StudyLanguage, session.UserId })\n            .ToListAsync(cancellationToken)",
        ".Select(usageEvent => new { usageEvent.StudyLanguage, usageEvent.UserId })\n            .ToListAsync(cancellationToken)",
        "GroupLanguageCounts(IEnumerable<string?> languages, Func<string?, string> normalizeLanguage)",
        "NormalizeMissingLanguage(string? language)",
        "NormalizeStudyLanguageForStatistics(string? language)",
        "NormalizeNativeLanguageForStatistics(string? language)",
        "NormalizeExplanationLanguageForStatistics(string? language)",
        "StudyLanguageConstants.IsSupported(normalizedStudyLanguage)",
        "? StudyLanguageConstants.ToCanonicalValue(normalizedStudyLanguage)\n            : UnknownLanguage",
        "GetExplanationLanguageDistributionAsync(CancellationToken cancellationToken)",
        "totalUsers == 0",
        "var totalInstallations = await dbContext.Devices.AsNoTracking().CountAsync(cancellationToken);",
        "Premium, Trial, subscription, billing, and access-status records are not counted.",
    ]:
        assert_contains(service, snippet, "EF-safe aggregate statistics pattern")

    for forbidden in [
        ".GroupBy(settings => settings.StudyLanguage == null || settings.StudyLanguage.Trim() == string.Empty",
        ".Union(usageLanguages)",
        "ExplanationLanguage = user.Settings == null ? null : user.Settings.ExplanationLanguage",
        "? user.ExplanationLanguage",
        "NativeLanguageCatalog.English.EnglishName",
        "user.Email",
        "PasswordHash",
    ]:
        assert_not_contains(service, forbidden, "privacy-safe EF statistics implementation")

    for snippet in [
        "const safePayload = payload && typeof payload === \"object\" ? payload : {};",
        "const items = Array.isArray(distribution) ? distribution : [];",
        "safePayload.selectedStudyLanguageDistribution || safePayload.studyLanguageDistribution || []",
        "safePayload.explanationLanguageDistribution || []",
        "No language data available.",
        "Tracked signed-in app/device records",
    ]:
        assert_contains(admin_js, snippet, "defensive admin statistics rendering")

    assert_contains(admin_html, "not raw installer downloads and not Premium or Trial access counts", "admin statistics boundary note")

    print("Admin product statistics policy checks passed.")


if __name__ == "__main__":
    main()
