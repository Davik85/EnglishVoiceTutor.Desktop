#!/usr/bin/env python3
"""Policy checks for Windows installer installed-version handling."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
INNO_SCRIPT = ROOT / "installer" / "windows" / "LanguageVoiceTutor.iss"
DOCS = [
    ROOT / "docs" / "WINDOWS_RELEASE_SERVER_UPLOAD.md",
    ROOT / "docs" / "TESTER_RELEASE.md",
    ROOT / "docs" / "CURRENT_STATE.md",
    ROOT / "docs" / "NEXT_STEPS.md",
]
MODIFIED_TEXT_FILES = [INNO_SCRIPT, *DOCS]

SENSITIVE_ASSIGNMENT_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|secret|password|passwd|pwd|token)\s*[:=]\s*['\"][^'\"]+['\"]"),
    re.compile(r"(?i)(bearer|basic)\s+[a-z0-9._~+/=-]{16,}"),
    re.compile(r"(?i)-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----"),
]


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Missing {label}: {pattern}")


def assert_no_sensitive_values(path: pathlib.Path) -> None:
    text = read(path)
    for pattern in SENSITIVE_ASSIGNMENT_PATTERNS:
        match = pattern.search(text)
        if match:
            raise AssertionError(
                f"Potential sensitive value found in {path.relative_to(ROOT)}: {match.group(0)}"
            )


def main() -> int:
    inno = read(INNO_SCRIPT)

    assert_contains(inno, "Uninstall\\LanguageVoiceTutor.Desktop_is1", "Inno uninstall registry key")
    assert_contains(inno, "RegQueryStringValue", "installed version registry lookup")
    assert_contains(inno, "DisplayVersion", "DisplayVersion lookup")
    assert_contains(inno, "Inno Setup: App Version", "Inno app version fallback lookup")
    assert_contains(inno, "CompareVersions", "version comparison function")
    assert_contains(inno, "ReadCoreSegment", "numeric SemVer core parser")
    assert_contains(inno, "ComparePrerelease", "tester prerelease parser")
    assert_contains(inno, "0 to 2", "major/minor/patch numeric comparison loop")
    assert_contains(
        inno,
        "Language Voice Tutor version ' + InstalledVersion + ' is already installed. Do you want to reinstall the same version?",
        "same-version reinstall confirmation text",
    )
    assert_regex(
        inno,
        r"VersionComparison\s*<\s*0.*older version.*Setup will update Language Voice Tutor",
        "older installed version update branch",
    )
    assert_regex(
        inno,
        r"VersionComparison\s*=\s*0.*MB_YESNO\)\s*=\s*IDYES",
        "same installed version confirmation branch",
    )
    assert_regex(
        inno,
        r"newer version.*may downgrade the app.*Result\s*:=\s*False",
        "newer installed version blocked branch",
    )
    assert_contains(inno, "CloseApplications=yes", "app-close behavior")
    assert_contains(inno, "CloseApplicationsFilter={#AppExeName}", "app-close executable filter")

    combined_docs = "\n".join(read(path) for path in DOCS)
    assert_contains(combined_docs, "Installed-version checking is now part of the Windows installer foundation", "installer version-check docs")
    assert_contains(combined_docs, "Same-version install asks for reinstall confirmation", "same-version docs")
    assert_contains(combined_docs, "Older installed version is treated as an update", "older update docs")
    assert_regex(combined_docs, r"Newer installed version (?:warns and blocks|warns/blocks)", "newer downgrade docs")
    assert_contains(combined_docs, "simple user-facing **Check for updates** button", "manual update UI docs")
    assert_contains(combined_docs, "latest.json", "latest.json docs")
    assert_contains(combined_docs, "SHA-256", "SHA-256 docs")
    assert_contains(combined_docs, "does not silently auto-update", "manual no silent update docs")
    assert_contains(combined_docs, "clean-machine smoke", "remaining clean-machine smoke docs")

    for path in MODIFIED_TEXT_FILES:
        assert_no_sensitive_values(path)

    print("Windows installer version-check policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
