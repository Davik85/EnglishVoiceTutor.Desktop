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
CURRENT_DIRECT_RELEASE = "1.1"
CURRENT_DIRECT_INSTALLER = "LanguageVoiceTutorSetup-1.1.exe"
CURRENT_BACKEND_RELEASE = "0.1.35-backend.138"
PREVIOUS_BACKEND_ROLLBACK_RELEASE = "0.1.35-backend.137"
STALE_BACKEND_RELEASES = ["0.1.35-backend.27", "0.1.35-backend.33", "0.1.35-backend.34"]
STALE_TESTER_RELEASES = ["0.1.35-tester.1", "0.1.36-tester.2", "0.1.36-tester.3", "0.1.36-tester.17"]
DEFERRED_ITEMS = [
    "Code signing remains deferred",
    "broad public paid launch remains pending",
    "Full-refund Premium revocation is production-verified",
    "expanded customer portal/subscription management is deferred",
    "CMS published-snapshot runtime is active for published Windows direct lessons",
    "backend deployment, database migrations, the download website, and update UI remain separate work",
    "Generated local files under `artifacts/`",
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
        r"public Windows direct release.*live website manifest|latest\.json.*public source of truth for the live Windows",
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
    assert_contains(combined_main, CURRENT_DIRECT_RELEASE, "current verified direct release")
    assert_contains(combined_main, CURRENT_DIRECT_INSTALLER, "current verified direct installer")
    assert_contains(combined_main, CURRENT_BACKEND_RELEASE, "current verified backend release")
    assert_contains(combined_main, "not broad public production readiness", "no broad production readiness wording")
    assert_contains(combined_main, "Do not state that the product is fully public production-ready", "no fully public production-ready wording")

    assert_contains(combined_main, CURRENT_BACKEND_RELEASE, "last verified active backend snapshot")
    assert_contains(combined_main, PREVIOUS_BACKEND_ROLLBACK_RELEASE, "rollback backend reference")
    assert_contains(combined_main, "Tracked signed-in app/device records", "current Admin statistics device metric label")
    assert_contains(combined_main, "DeviceEntity", "Admin statistics device metric source")
    assert_contains(combined_main, "not raw installer downloads", "Admin statistics device metric excludes raw downloads")
    assert_contains(combined_main, "Successful payments current month", "deployed payment statistics current-month metric")
    assert_contains(combined_main, "RateLimiting__Enabled=true", "production rate limiting enabled flag")
    assert_contains(
        combined_main,
        "AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false",
        "production admin bootstrap fallback disabled flag",
    )
    assert_regex(
        combined_main,
        r"Phase 3[^\n]*(?:completed|implemented)[^\n]*production-verified|Completed and production-verified[^\n]*Phase 3",
        "Phase 3 completed and production-verified wording",
    )
    assert_regex(
        combined_main,
        r"Phase 4A[^\n]*backup/readability/separate-drill-restore[^\n]*(?:completed|was completed)|Completed[^\n]*Phase 4A[^\n]*backup/readability/separate-drill-restore",
        "Phase 4A backup/readability/separate-drill-restore completed wording",
    )
    assert_regex(
        combined_main,
        r"Phase 4B[^\n]*(?:local PostgreSQL backup scheduling|local scheduled PostgreSQL backups)[^\n]*(?:active|activated|installed)",
        "Phase 4B local scheduled PostgreSQL backups active wording",
    )
    assert_not_regex(combined_main, r"off-server encrypted backups[^\n]*(?:complete|completed|done|active)", "off-server encrypted backups complete claim")
    assert_regex(
        combined_main,
        r"Phase 4D[^\n]*permission-fidelity restore drill[^\n]*(?:completed|complete|passed)|permission-fidelity restore drill[^\n]*(?:completed|complete|passed)",
        "Phase 4D permission-fidelity restore drill completed wording",
    )
    assert_regex(
        combined_main,
        r"Phase 4C[^\n]*migration rollback/remediation[^\n]*(?:completed|complete|passed)|migration rollback/remediation[^\n]*dry-run rehearsal[^\n]*(?:completed|complete|passed)",
        "Phase 4C migration rollback/remediation dry-run rehearsal completed wording",
    )
    assert_contains(combined_main, "Phase 4 is complete for the current release-readiness level", "Phase 4 current release-readiness completion wording")
    assert_contains(combined_main, "Off-server encrypted backups remain optional future infrastructure hardening", "off-server encrypted backups optional future hardening wording")

    for item in DEFERRED_ITEMS:
        assert_contains(combined_main, item, f"deferred item: {item}")

    stale_current_context = (
        r"(?:current|live|public|verified|baseline|snapshot|latest active|in place|manifest should point|"
        r"Last verified public snapshot|Current verified manifest baseline)"
    )
    for stale_version in STALE_TESTER_RELEASES:
        assert_not_regex(
            combined_all,
            rf"{stale_current_context}[^\n]*{re.escape(stale_version)}(?![0-9])|{re.escape(stale_version)}(?![0-9])[^\n]*(?:current|live|public|verified|baseline|snapshot|latest active|in place)",
            f"stale tester version used as current/live/public baseline: {stale_version}",
        )

    assert_contains(combined_main, PREVIOUS_BACKEND_ROLLBACK_RELEASE, "current rollback backend release")
    for stale_backend in STALE_BACKEND_RELEASES:
        assert_not_regex(
            combined_all,
            rf"{stale_current_context}[^\n]*{re.escape(stale_backend)}(?![0-9])|{re.escape(stale_backend)}(?![0-9])[^\n]*(?:current|live|public|verified|baseline|snapshot|latest active|in place|active via|deployed and healthy)",
            f"stale backend version used as current/live/public baseline: {stale_backend}",
        )
    assert_not_regex(
        combined_all,
        r"artifacts[/\\][^\n.]*current public tester|current public tester[^\n.]*artifacts[/\\]",
        "artifacts treated as current public release",
    )


    current_state = read(ROOT / "docs" / "CURRENT_STATE.md")
    next_steps = read(ROOT / "docs" / "NEXT_STEPS.md")
    assert_not_regex(current_state, r"(?:current|live|active|deployed and healthy)[^\n]*0\.1\.35-backend\.27", "old backend 0.1.35-backend.27 claimed current")
    assert_not_regex(current_state, r"0\.1\.36-tester\.17[^\n]*(?:current|live|active|latest)", "old tester 0.1.36-tester.17 claimed current")
    assert_not_regex(next_steps, r"immediate blocker[^\n]*(?:billing UI localization|cancel-renewal)|(?:billing UI localization|cancel-renewal)[^\n]*immediate blocker", "completed billing UI localization/cancel-renewal listed as immediate blocker")
    assert_contains(combined_main, "public Windows direct release, not a full broad production-readiness claim", "Windows direct release not full production readiness wording")
    required_billing_truths = [
        ("Controlled Paddle live payment validation completed", "controlled Paddle live payment completed wording"),
        ("desktop cancel-renewal validation", "cancel-renewal validation completed wording"),
        ("failed payment attempts did not grant Premium", "failed payments do not grant Premium wording"),
        (f"current backend release is `{CURRENT_BACKEND_RELEASE}`", "current production backend wording"),
        ("Full-refund Premium revocation is production-verified", "full refund production verified wording"),
        ("chargeback remains implemented/test-covered but not live-chargeback-tested", "chargeback not overclaimed wording"),
        ("expanded customer portal/subscription management is deferred", "customer portal deferred wording"),
        ("broad public paid launch remains pending", "broad public paid launch pending wording"),
        ("Direct installer code signing remains pending", "direct installer code signing pending wording"),
    ]
    for needle, label in required_billing_truths:
        assert_contains(combined_main, needle, label)
    assert_not_regex(
        combined_main,
        r"Production/live Paddle readiness remains deferred(?![^\n]*(?:broad public paid launch|controlled live payment validation))",
        "outdated blanket Paddle live deferred wording",
    )
    assert_regex(combined_main, r"Windows direct-release upload publishes static release files only\. It does not deploy the backend, does not run EF migrations", "Windows upload separate from backend/migrations wording")
    assert_regex(combined_main, r"backend upload/package scripts do not apply EF migrations automatically|Backend deploys remain separate from EF database migrations", "migrations explicit not upload-script wording")
    assert_regex(combined_main, r"Generated local files under `artifacts/`.*must not be committed|Generated artifacts.*must not be committed", "generated artifacts not committed wording")

    print("Documentation source-of-truth policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

