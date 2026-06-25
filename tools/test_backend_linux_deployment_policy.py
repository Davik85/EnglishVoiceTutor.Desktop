#!/usr/bin/env python3
"""Policy checks for the self-contained Linux backend deployment workflow."""
from __future__ import annotations

import re
import shutil
import subprocess
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


def assert_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, flags=re.MULTILINE) is None:
        raise AssertionError(f"Missing {label}: {pattern}")


def assert_no_bash_escaped_quotes_in_powershell_strings(text: str) -> None:
    """Reject parser-breaking bash-style \" escapes inside PowerShell double-quoted strings."""
    for line_number, line in enumerate(text.splitlines(), start=1):
        index = 0
        while index < len(line):
            if line[index] != '"':
                index += 1
                continue

            index += 1
            while index < len(line):
                char = line[index]
                next_char = line[index + 1] if index + 1 < len(line) else ""
                if char == "`":
                    index += 2
                    continue
                if char == '"' and next_char == '"':
                    index += 2
                    continue
                if char == "\\" and next_char == '"':
                    raise AssertionError(
                        "PowerShell double-quoted strings must not use bash-style "
                        f"\\\" escaping because it can break parsing (line {line_number})."
                    )
                if char == '"':
                    break
                index += 1
            index += 1


def assert_powershell_parser_accepts(relative: str) -> None:
    executable = shutil.which("pwsh") or shutil.which("powershell")
    if executable is None:
        print(f"Skipping PowerShell parser check for {relative}: pwsh/powershell not available.")
        return

    script_path = ROOT / relative
    parser_command = (
        "$tokens = $null; "
        "$errors = $null; "
        f"[System.Management.Automation.Language.Parser]::ParseFile('{script_path.as_posix()}', [ref]$tokens, [ref]$errors) | Out-Null; "
        "if ($errors.Count -gt 0) { "
        "$errors | ForEach-Object { Write-Error $_.Message }; "
        "exit 1 "
        "}"
    )
    result = subprocess.run(
        [executable, "-NoProfile", "-Command", parser_command],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise AssertionError(
            f"PowerShell parser rejected {relative}.\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}"
        )


def main() -> None:
    package_script = read("scripts/package-backend-linux-release.ps1")
    upload_script = read("scripts/upload-backend-linux-release.ps1")
    migration_script = read("scripts/generate-backend-refresh-token-migration-sql.ps1")
    website_cms_migration_script = read("scripts/generate-backend-website-cms-migration-sql.ps1")
    docs = read("docs/BACKEND_SERVER_DEPLOYMENT.md")

    assert_no_bash_escaped_quotes_in_powershell_strings(upload_script)
    assert_powershell_parser_accepts("scripts/upload-backend-linux-release.ps1")
    assert_powershell_parser_accepts("scripts/generate-backend-website-cms-migration-sql.ps1")

    for needle in ["-r", "linux-x64", "--self-contained", "true", "PublishSingleFile=false"]:
        assert_contains(package_script, needle, "self-contained linux-x64 package publish")

    for needle in [
        "$ServerHost = 'lvt-server'",
        "$ServerUser = 'deploy'",
        "$RemotePath = '/opt/languagevoicetutor/backend'",
        "[switch]$DryRun",
        "[switch]$PackageFirst",
        "[switch]$NoRestart",
        "$remoteBase/uploads/$Version",
        "$remoteBase/releases",
        "$remoteBase/current",
        "$remoteBase/previous",
        "deploy-backend-release.sh",
        "#!/usr/bin/env bash",
        "set -euo pipefail",
        '$remoteDeployScriptPath = "$remoteUploadDir/deploy-backend-release.sh"',
        "scp",
        "bash $(Quote-ForRemoteShell $remoteDeployScriptPath)",
        "previous_target=''",
        "readlink -f",
        'ln -sfn "`$previous_target" "`$previous_link"',
        "mv -Tf",
        "chmod 755",
        "EnglishVoiceTutor.Api",
        "$shouldRestartService = -not $NoRestart",
        "Dry-run mode: sudo restart/status commands will be printed with SSH pseudo-terminal allocation (-tt) when restart is enabled.",
        "@('ssh', '-tt', '-p', $SshPort.ToString(), $serverTarget, \"sudo systemctl restart $serviceName\")",
        "@('ssh', '-tt', '-p', $SshPort.ToString(), $serverTarget, \"sudo systemctl status $serviceName --no-pager\")",
        "This script does not write secrets and does not run EF migrations",
    ]:
        assert_contains(upload_script, needle, "safe upload workflow")


    assert_regex(
        upload_script,
        r"if \(\$shouldRestartService\) \{\s+Invoke-LoggedCommand -Command @\('ssh', '-tt', '-p', \$SshPort\.ToString\(\), \$serverTarget, \"sudo systemctl restart \$serviceName\"\)\s+Invoke-LoggedCommand -Command @\('ssh', '-tt', '-p', \$SshPort\.ToString\(\), \$serverTarget, \"sudo systemctl status \$serviceName --no-pager\"\)\s+\}\s+else \{\s+Write-Host \"Service restart skipped because -NoRestart was provided\.\"",
        "-NoRestart-gated TTY sudo restart/status block",
    )
    assert_not_contains(upload_script, "sudo -S", "sudo password stdin handling")

    for forbidden in [
        "bash -lc",
        "remoteDeployScript = @(",
        " -join ' && '",
        "git pull",
        "dotnet build",
        "dotnet ef database update",
        "Database=",
        "Password=",
        "ConnectionStrings__DefaultConnection",
        "PGPASSWORD",
        "\\\"",
    ]:
        assert_not_contains(upload_script, forbidden, "server-side build, secret handling, parser-breaking quoting, or inline deploy script")

    for needle in [
        "[ValidatePattern('^[A-Za-z0-9._-]+$')]",
        "artifacts/temp/backend-linux-upload/$Version",
    ]:
        assert_contains(upload_script, needle, "version validation and ignored generated deploy script path")

    gitignore = read(".gitignore")
    assert_contains(gitignore, "artifacts/", "ignored generated deploy scripts and artifacts")

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
        "20260625090000_AddWebsiteCmsLegalContentFoundation",
        "20260620165657_AddAdminRoleAssignmentPersistence",
        "website_cms_sections",
        "dotnet",
        "ef",
        "migrations",
        "script",
        "artifacts/sql/backend",
        "does not apply SQL to any database",
        "does not connect to production",
        "does not read or print database secrets",
    ]:
        assert_contains(website_cms_migration_script, needle, "Website CMS local migration SQL generation")

    for forbidden in [
        "dotnet ef database update",
        "database update",
        "psql",
        "PGPASSWORD",
        "ConnectionStrings__DefaultConnection",
        "Database=",
        "Password=",
        "Host=",
        "Username=",
        "Paddle__",
        "OpenAI",
        "Jwt",
        "JWT",
        "webhook secret",
    ]:
        assert_not_contains(website_cms_migration_script, forbidden, "Website CMS SQL generator database apply command, connection string, or secret")

    for needle in [
        "The production server does not need a git checkout",
        "`dotnet` SDK",
        "`dotnet` runtime",
        "lvt-server",
        "/opt/languagevoicetutor/backend/releases/<version>",
        "languagevoicetutor-backend.service",
        "`ssh -tt`",
        "the script does not run the sudo restart or sudo status commands",
        "printed but not executed",
        "20260611000000_AddUserRefreshTokens",
        "psql",
        "Do not echo `ConnectionStrings__DefaultConnection`, `PGPASSWORD`, or database URLs",
        "https://api.languagevoicetutor.com",
    ]:
        assert_contains(docs, needle, "deployment documentation")

    print("Backend Linux deployment policy checks passed.")


if __name__ == "__main__":
    main()
