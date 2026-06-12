#!/usr/bin/env python3
"""Policy checks for documentation source-of-truth wording."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
MAIN_DOCS = [
    ROOT / "README.md",
    ROOT / "docs" / "CURRENT_STATE.md",
    ROOT / "docs" / "NEXT_STEPS.md",
    ROOT / "docs" / "TESTER_RELEASE.md",
    ROOT / "docs" / "WINDOWS_INSTALLER_RELEASE_FLOW.md",
    ROOT / "docs" / "WINDOWS_INSTALLER_UPDATE_FLOW.md",
    ROOT / "docs" / "WINDOWS_RELEASE_SERVER_UPLOAD.md",
    ROOT / "docs" / "BACKEND_SERVER_DEPLOYMENT.md",
    ROOT / "docs" / "LOCAL_RELEASE.md",
]
ALL_MARKDOWN = [ROOT / "README.md", *sorted((ROOT / "docs").glob("*.md"))]
LATEST_JSON_COMMAND = "Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json"
BACKEND_SYMLINK_COMMAND = 'ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"'
PROD_BACKEND_URL = "https://api.languagevoicetutor.com"
LOCAL_BUILD = "0.1.36-tester.2"
DEFERRED_ITEMS = [
    "Code signing remains deferred",
    "Production billing/Paddle/subscription payment lifecycle remains deferred",
    "CMS published-snapshot learner runtime remains disabled/not the default",
]


def read(path: pathlib.Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing expected file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Missing {label}: {pattern}")


def assert_not_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Forbidden {label}: {pattern}")


def main() -> int:
    combined_main = "\n".join(read(path) for path in MAIN_DOCS)
    combined_all = "\n".join(read(path) for path in ALL_MARKDOWN)

    assert_contains(combined_main, "Source of truth for current versions", "source-of-truth section")
    assert_regex(
        combined_main,
        r"public Windows direct tester release.*live website manifest|latest\.json.*public source of truth for the live Windows",
        "live Windows latest.json source-of-truth wording",
    )
    assert_regex(
        combined_main,
        r"production backend release.*server `current` symlink|Verify the live value .*server symlink",
        "backend symlink source-of-truth wording",
    )
    assert_contains(combined_main, LATEST_JSON_COMMAND, "latest.json verification command")
    assert_contains(combined_main, BACKEND_SYMLINK_COMMAND, "backend symlink verification command")
    assert_contains(
        combined_main,
        "Generated local files under `artifacts/` are not proof that a version is live on the public site.",
        "generated artifacts not proof/source of truth wording",
    )
    assert_contains(
        combined_main,
        "A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.",
        "local installer public only after upload/latest.json verification wording",
    )
    assert_contains(combined_main, PROD_BACKEND_URL, "release backend lock URL")
    assert_contains(combined_main, "Release/tester installed builds are server-only", "release backend lock wording")
    assert_contains(combined_main, "not broad public production readiness", "no broad production readiness wording")
    assert_contains(combined_main, "Do not state that the product is fully public production-ready", "no fully public production-ready wording")

    for item in DEFERRED_ITEMS:
        assert_contains(combined_main, item, f"deferred item: {item}")

    assert_regex(
        combined_all,
        rf"{re.escape(LOCAL_BUILD)}.*not (?:be described as )?public/live|not public/live.*{re.escape(LOCAL_BUILD)}|{re.escape(LOCAL_BUILD)}.*public/live only after",
        "local build is not public/live without upload/latest.json verification",
    )
    assert_not_regex(
        combined_all,
        rf"{re.escape(LOCAL_BUILD)}[^\n.]*is (?:the )?(?:current )?(?:public|live)(?![^\n.]*only after)",
        "local artifacts build claimed as public/live without an only-after-upload condition",
    )
    assert_not_regex(
        combined_all,
        r"artifacts[/\\][^\n.]*current public tester|current public tester[^\n.]*artifacts[/\\]",
        "artifacts treated as current public release",
    )

    print("Documentation source-of-truth policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
