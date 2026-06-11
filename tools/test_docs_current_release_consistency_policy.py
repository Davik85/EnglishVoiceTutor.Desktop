#!/usr/bin/env python3
"""Policy checks for current release documentation consistency."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
DOCS = [
    ROOT / "README.md",
    ROOT / "docs" / "CURRENT_STATE.md",
    ROOT / "docs" / "NEXT_STEPS.md",
    ROOT / "docs" / "TESTER_RELEASE.md",
    ROOT / "docs" / "WINDOWS_INSTALLER_RELEASE_FLOW.md",
    ROOT / "docs" / "WINDOWS_INSTALLER_UPDATE_FLOW.md",
    ROOT / "docs" / "WINDOWS_RELEASE_SERVER_UPLOAD.md",
    ROOT / "docs" / "LOCAL_RELEASE.md",
]

REQUIRED = [
    "0.1.26-tester.1",
    "LanguageVoiceTutorSetup-0.1.26-tester.1.exe",
    "https://api.languagevoicetutor.com",
    "server-only",
    "Check for updates",
    "SHA-256",
    "does not silently auto-update",
]

FORBIDDEN_PATTERNS = [
    (re.compile(r"update UI (?:is )?not implemented", re.I), "stale update UI not implemented wording"),
    (re.compile(r"automatic update UX is not implemented", re.I), "stale automatic update UX wording"),
    (re.compile(r"current app does not (?:check|fetch|read).*latest\.json", re.I | re.S), "stale app does not read manifest wording"),
    (re.compile(r"tester/release users can edit Backend URL", re.I), "release Backend URL editing"),
    (re.compile(r"Diagnostics tab is required for normal users", re.I), "Diagnostics requirement for normal users"),
    (re.compile(r"localhost is used in release", re.I), "localhost release backend wording"),
    (re.compile(r"0\.1\.(?:13|19|20|21|22|23|25|17|8)[^\n]*(?:current|latest|next Windows installer package)", re.I), "old current/latest version wording"),
]


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def main() -> int:
    combined = "\n".join(read(path) for path in DOCS)
    for needle in REQUIRED:
        if needle not in combined:
            raise AssertionError(f"Missing required current-release documentation: {needle}")

    for path in DOCS:
        text = read(path)
        for pattern, label in FORBIDDEN_PATTERNS:
            match = pattern.search(text)
            if match:
                raise AssertionError(f"Forbidden {label} in {path.relative_to(ROOT)}: {match.group(0)!r}")

    print("Docs current release consistency policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
