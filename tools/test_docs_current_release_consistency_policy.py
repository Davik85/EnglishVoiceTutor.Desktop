#!/usr/bin/env python3
"""Policy checks for current release documentation consistency."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
COMMAND_PLAYBOOK = ROOT / "docs" / "COMMAND_PLAYBOOK.md"
SMOKE_GATE = ROOT / "docs" / "desktop-release-smoke-gate.md"
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

REQUIRED_BY_DOCUMENT = {
    "README.md": ["current verified public direct build is `1.5`", "LanguageVoiceTutorSetup-1.5.exe", "dea33ac29414d5956db52cec0dd703ecb12778e071c1e601dcf394f1def2e10b", "0.1.35-backend.140", "does not silently auto-update"],
    "docs/CURRENT_STATE.md": ["Windows Direct Release 1.5", "LanguageVoiceTutorSetup-1.5.exe", "dea33ac29414d5956db52cec0dd703ecb12778e071c1e601dcf394f1def2e10b", "0.1.35-backend.140", "Google Play remains disabled", "No backend deployment, EF migration, database change"],
    "docs/NEXT_STEPS.md": ["current Windows direct public release is `1.5`", "LanguageVoiceTutorSetup-1.5.exe", "endpoint remains disabled", "version-specific shortcut-icon"],
    "docs/TESTER_RELEASE.md": ["Windows Direct Release 1.5", "LanguageVoiceTutorSetup-1.5.exe", "0.1.35-backend.140"],
    "docs/WINDOWS_INSTALLER_RELEASE_FLOW.md": ["LanguageVoiceTutorSetup-1.5.exe", "0.1.35-backend.140", "manual-confirmation", "app-icon-{AppVersion}.ico"],
    "docs/WINDOWS_INSTALLER_UPDATE_FLOW.md": ["LanguageVoiceTutorSetup-1.5.exe", "version: 1.5", "does not silently auto-update"],
    "docs/WINDOWS_RELEASE_SERVER_UPLOAD.md": ["version: 1.5", "LanguageVoiceTutorSetup-1.5.exe", "dea33ac29414d5956db52cec0dd703ecb12778e071c1e601dcf394f1def2e10b", "no backend deployment, migration, or database change"],
    "docs/LOCAL_RELEASE.md": ["$ReleaseVersion = \"<release-version>\"", "Windows Direct 1.5 passed"],
}

REQUIRED_SHARED = ["https://api.languagevoicetutor.com", "server-only", "Check for updates", "SHA-256", "does not silently auto-update"]

FORBIDDEN_PATTERNS = [
    (re.compile(r"(?:current|live|active|deployed and healthy)[^\n]*0\.1\.35-backend\.27", re.I), "old backend current wording"),
    (re.compile(r"0\.1\.36-tester\.17[^\n]*(?:current|live|active|latest)", re.I), "old tester current wording"),
    (re.compile(r"(?:current|active|latest verified|last verified public snapshot|current verified manifest baseline)[^\n]*(?:Windows Direct (?:Release )?1\.[0-4]|version[:= ]+1\.[0-4]|LanguageVoiceTutorSetup-1\.[0-4]\.exe)", re.I), "old Windows release current wording"),
    (re.compile(r"Current public Windows Direct\s+`?1\.[0-4]`?", re.I), "old Windows current wording"),
    (re.compile(r"(?:current|active|production backend)[^\n]*0\.1\.35-backend\.99", re.I), "old backend current wording"),
    (re.compile(r"Google Play (?:is )?enabled", re.I), "Google Play enabled wording"),
    (re.compile(r"Windows (?:Direct )?1\.5[^\n]*(?:backend deployment|migration).*(?:required|performed)", re.I), "Windows 1.5 backend or migration claim"),
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
    texts = {path.relative_to(ROOT).as_posix(): read(path) for path in DOCS}
    for relative_path, needles in REQUIRED_BY_DOCUMENT.items():
        text = texts[relative_path]
        for needle in needles:
            if needle not in text:
                raise AssertionError(f"Missing required current-release documentation in {relative_path}: {needle}")
    combined = "\n".join(texts.values())
    for needle in REQUIRED_SHARED:
        if needle not in combined:
            raise AssertionError(f"Missing required current-release documentation: {needle}")

    release_documents = [ROOT / "README.md", *DOCS, COMMAND_PLAYBOOK, SMOKE_GATE]
    release_text = "\n".join(read(path) for path in release_documents)
    if re.search(r"upload-windows-direct-release\.ps1[^\n]*-Version", release_text, re.I):
        raise AssertionError("Windows upload documentation must not pass unsupported -Version to upload-windows-direct-release.ps1")
    command_playbook = read(COMMAND_PLAYBOOK)
    smoke_gate = read(SMOKE_GATE)
    for needle in [
        "LanguageVoiceTutorSetup-1.5.exe",
        "-ServerHost lvt-server",
        "-ServerUser deploy",
        "-RemotePath /var/www/languagevoicetutor/releases/windows/direct",
    ]:
        if needle not in command_playbook:
            raise AssertionError(f"Command Playbook missing current Windows upload contract: {needle}")
    if "-DryRun" not in smoke_gate or "Do not run the real upload before local validation and dry-run review succeed." not in smoke_gate:
        raise AssertionError("Desktop smoke gate must require dry-run review before real Windows upload")

    for relative_path in ["docs/NEXT_STEPS.md", "docs/WINDOWS_RELEASE_SERVER_UPLOAD.md"]:
        text = texts[relative_path]
        if re.search(r"(?:current|live)[^\n]*0\.1\.35-backend\.(?:112|99|24)", text, re.I):
            raise AssertionError(f"Stale current backend wording in {relative_path}")

    for relative_path in ["docs/CURRENT_STATE.md", "docs/NEXT_STEPS.md"]:
        text = texts[relative_path]
        if re.search(r"current Windows (?:direct )?(?:public )?release is visible without JavaScript", text, re.I):
            raise AssertionError(f"Unverified no-JavaScript current-release claim in {relative_path}")
        if "static/no-JavaScript fallback was not separately verified by this Windows release upload" not in text:
            raise AssertionError(f"Missing no-JavaScript verification boundary in {relative_path}")

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
