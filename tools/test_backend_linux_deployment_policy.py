#!/usr/bin/env python3
"""Policy checks for the self-contained Linux backend deployment workflow."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.exists():
        raise AssertionError(f"Missing required file: {relative}")
    return path.read_text(encoding="utf-8-sig")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    package_script = read("scripts/package-backend-linux-release.ps1")
    upload_script = read("scripts/upload-backend-linux-release.ps1")
    migration_script = read("scripts/generate-backend-refresh-token-migration-sql.ps1")
    docs = read("docs/BACKEND_SERVER_DEPLOYMENT.md")

    for needle in ["-r", "linux-x64", "--self-contained", "true", "PublishSingleFile=false"]:
        assert_contains(package_script, needle, "self-contained linux-x64 package publish")

    for needle in [
        "$ServerHost = 'lvt-server'",
        "$ServerUser = 'deploy'",
        "$RemotePath = '/opt/languagevoicetutor/backend'",
        "$remoteBase/releases",
        "$remoteBase/current",
        "$remoteBase/previous",
        "mv -Tf",
        "sudo systemctl restart $serviceName",
        "sudo systemctl status $serviceName --no-pager",
        "This script does not write secrets and does not run EF migrations",
    ]:
        assert_contains(upload_script, needle, "safe upload workflow")

    for forbidden in ["git pull", "dotnet build", "dotnet ef database update", "Password="]:
        assert_not_contains(upload_script, forbidden, "server-side build or secret handling")

    for needle in [
        "20260611000000_AddUserRefreshTokens",
        "20260604121000_AddCmsDraftSaveAuditMetadata",
        "dotnet",
        "ef",
        "migrations",
        "script",
        "artifacts/sql/backend",
        "does not connect to production and does not read or print database secrets",
    ]:
        assert_contains(migration_script, needle, "local migration SQL generation")

    for needle in [
        "The production server does not need a git checkout",
        "`dotnet` SDK",
        "`dotnet` runtime",
        "lvt-server",
        "/opt/languagevoicetutor/backend/releases/<version>",
        "languagevoicetutor-backend.service",
        "20260611000000_AddUserRefreshTokens",
        "psql",
        "Do not echo `ConnectionStrings__DefaultConnection`, `PGPASSWORD`, or database URLs",
        "https://api.languagevoicetutor.com",
    ]:
        assert_contains(docs, needle, "deployment documentation")

    print("Backend Linux deployment policy checks passed.")


if __name__ == "__main__":
    main()
