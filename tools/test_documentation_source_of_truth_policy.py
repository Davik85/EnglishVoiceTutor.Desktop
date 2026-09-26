#!/usr/bin/env python3
"""Durable source-of-truth and documentation ownership checks."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
REMOVED = {
    "CODEX_AND_GPT_5_6_DEVELOPMENT.md",
    "DOCUMENTATION_REVIEW.md",
    "CODEBASE_REVIEW.md",
    "STABILIZATION_PLAN.md",
    "RELEASE_READINESS_REVIEW.md",
    "desktop-release-readiness-audit.md",
    "desktop-release-work-plan.md",
    "PRE_MOBILE_READINESS.md",
    "MOBILE_V1_PLANNING.md",
}
LIVE_MANIFEST = "Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json"
BACKEND_SYMLINK = 'ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"'
PROD_URL = "https://api.languagevoicetutor.com"


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        raise AssertionError(f"Missing required document: {relative}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def main() -> int:
    readme = read("README.md")
    current = read("docs/CURRENT_STATE.md")
    next_steps = read("docs/NEXT_STEPS.md")
    playbook = read("docs/COMMAND_PLAYBOOK.md")
    backend = read("docs/BACKEND_SERVER_DEPLOYMENT.md")
    release = read("docs/WINDOWS_INSTALLER_RELEASE_FLOW.md")
    update = read("docs/WINDOWS_INSTALLER_UPDATE_FLOW.md")
    upload = read("docs/WINDOWS_RELEASE_SERVER_UPLOAD.md")
    tester = read("docs/TESTER_RELEASE.md")
    all_docs = {"README.md": readme, **{f"docs/{p.name}": p.read_text(encoding="utf-8") for p in DOCS.glob("*.md")}}
    combined_operations = "\n".join((current, next_steps, playbook, backend, release, update, upload, tester))

    for name in REMOVED:
        if (DOCS / name).exists():
            raise AssertionError(f"Superseded document still exists: {name}")
        for relative, content in all_docs.items():
            if name in content:
                raise AssertionError(f"{relative} still points to removed document {name}")

    for relative, content in all_docs.items():
        if re.search(r"Built with Codex|Codex was used|GPT-5\.6 was used|AI-assisted engineering workflow", content, re.I):
            raise AssertionError(f"Development-workflow disclosure remains in {relative}")

    for link in ("docs/CURRENT_STATE.md", "docs/COMMAND_PLAYBOOK.md", "docs/BACKEND_SERVER_DEPLOYMENT.md", "docs/WINDOWS_INSTALLER_RELEASE_FLOW.md"):
        require(readme, f"]({link})", f"README link to {link}")
    if re.search(r"0\.1\.35-backend\.\d+|LanguageVoiceTutorSetup-\d|versionCode\s+\d+", readme):
        raise AssertionError("README must not pin mutable release versions")

    for text, label in ((current, "current state"), (release, "installer flow"), (upload, "release upload")):
        require(text, LIVE_MANIFEST, f"live Windows manifest command in {label}")
        require(text, BACKEND_SYMLINK, f"backend symlink command in {label}")
    require(combined_operations, "Generated local files under `artifacts/` are not proof", "generated artifact boundary")
    require(combined_operations, "`latest.json` is verified over HTTPS", "public release verification boundary")
    require(combined_operations, "Release/tester installed builds are server-only", "installed backend lock")
    require(combined_operations, PROD_URL, "production backend URL")
    require(combined_operations, "does not silently auto-update", "manual update behavior")
    require(upload, "does not deploy the backend, does not run EF migrations", "Windows upload isolation")
    require(backend, "Backend/Admin CMS deployment is separate from database migration", "migration isolation")
    for needle, label in (
        ("Tracked signed-in app/device records", "Admin device metric label"),
        ("DeviceEntity", "backend device metric source"),
        ("not raw installer downloads", "download/device distinction"),
        ("Successful payments current month", "payment statistics metric"),
        ("RateLimiting__Enabled=true", "production rate limit flag"),
        ("AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false", "Admin fallback cutover"),
        ("Phase 4 is complete for the current release-readiness level", "backup and restore readiness"),
        ("Off-server encrypted backups remain optional future infrastructure hardening", "off-server backup boundary"),
        ("Full-refund Premium revocation is production-verified", "Paddle refund evidence"),
        ("chargeback remains implemented/test-covered but not live-chargeback-tested", "chargeback evidence boundary"),
        ("expanded customer portal/subscription management is deferred", "customer portal status"),
        ("broad public paid launch remains pending", "paid launch boundary"),
    ):
        require(combined_operations, needle, label)
    if re.search(r"off-server encrypted backups[^\n]*(?:complete|completed|done|active)", combined_operations, re.I):
        raise AssertionError("Off-server encrypted backups must not be claimed complete")
    for relative, content in (("docs/CURRENT_STATE.md", current), ("docs/BACKEND_SERVER_DEPLOYMENT.md", backend), ("docs/COMMAND_PLAYBOOK.md", playbook)):
        require(content, "gpt-5.6-luna", f"runtime AI model setting in {relative}")
    for text, label in ((current, "current state"), (next_steps, "next steps")):
        require(text, "Google Play Production", f"public Android state in {label}")

    print("Documentation source-of-truth policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
