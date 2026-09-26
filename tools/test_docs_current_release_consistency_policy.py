#!/usr/bin/env python3
"""Check current-facing release docs without pinning a release in every file."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def main() -> int:
    current = read("docs/CURRENT_STATE.md")
    next_steps = read("docs/NEXT_STEPS.md")
    backend = read("docs/BACKEND_SERVER_DEPLOYMENT.md")
    release = read("docs/WINDOWS_INSTALLER_RELEASE_FLOW.md")
    update = read("docs/WINDOWS_INSTALLER_UPDATE_FLOW.md")
    upload = read("docs/WINDOWS_RELEASE_SERVER_UPLOAD.md")
    local = read("docs/LOCAL_RELEASE.md")
    tester = read("docs/TESTER_RELEASE.md")
    playbook = read("docs/COMMAND_PLAYBOOK.md")
    smoke_gate = read("docs/desktop-release-smoke-gate.md")
    readme = read("README.md")

    # The leading current checkpoint may change; operational summaries must follow it.
    match = re.search(r"Production backend `(0\.1\.35-backend\.\d+)` is current", current[:3000])
    if not match:
        raise AssertionError("CURRENT_STATE.md needs a leading current backend checkpoint")
    backend_version = match.group(1)
    require(next_steps[:2200], backend_version, "current backend in Next Steps")
    require(backend[:2200], backend_version, "current backend in deployment runbook")

    for relative, text in (("README.md", readme), ("docs/NEXT_STEPS.md", next_steps), ("docs/WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md", read("docs/WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md"))):
        if re.search(r"pre-mobile planning (?:is|should)|mobile apps are not currently available|Google Play remains disabled|endpoint remains disabled", text, re.I):
            raise AssertionError(f"Obsolete current-state planning claim in {relative}")

    for text, label in ((release, "installer release"), (upload, "server upload"), (local, "local release"), (tester, "tester release")):
        require(text, "latest.json", f"manifest boundary in {label}")
        require(text, "https://api.languagevoicetutor.com", f"backend lock in {label}")
    require(release, "manual-confirmation", "Windows manifest update mode")
    require(update, "does not silently auto-update", "manual update policy")
    require(tester, "asks before download/install", "tester confirmation contract")
    require(tester, "verifies SHA-256", "tester installer hash contract")
    require(upload, "-DryRun", "Windows upload dry-run")
    require(smoke_gate, "-DryRun", "smoke gate dry-run")
    require(smoke_gate, "Do not run the real upload before local validation and dry-run review succeed.", "dry-run gate before upload")
    for needle in ("-ServerHost lvt-server", "-ServerUser deploy", "-RemotePath /var/www/languagevoicetutor/releases/windows/direct"):
        require(playbook, needle, "Windows upload command contract")

    release_text = "\n".join((readme, current, next_steps, release, update, upload, local, tester, playbook, smoke_gate))
    if re.search(r"upload-windows-direct-release\.ps1[^\n]*-Version", release_text, re.I):
        raise AssertionError("Windows upload docs pass unsupported -Version parameter")
    if re.search(r"tester/release users can edit Backend URL|localhost is used in release|update UI (?:is )?not implemented", release_text, re.I):
        raise AssertionError("Obsolete Windows release behavior claim")
    for text, label in ((current, "current state"), (next_steps, "next steps")):
        require(text, "static/no-JavaScript fallback was not separately verified by this Windows release upload", f"no-JavaScript evidence boundary in {label}")

    print("Docs current release consistency policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
