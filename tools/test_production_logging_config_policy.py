#!/usr/bin/env python3
"""Policy checks for release-ready production logging level hardening."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION_CONFIG = ROOT / "backend" / "EnglishVoiceTutor.Api" / "appsettings.Production.json"
SAFE_LEVELS = {"Warning", "Error", "Critical", "None"}


def main() -> None:
    if not PRODUCTION_CONFIG.exists():
        raise AssertionError(f"Missing required production config: {PRODUCTION_CONFIG.relative_to(ROOT)}")

    config = json.loads(PRODUCTION_CONFIG.read_text(encoding="utf-8"))
    log_levels = config.get("Logging", {}).get("LogLevel", {})

    ef_command_level = log_levels.get("Microsoft.EntityFrameworkCore.Database.Command")
    if ef_command_level not in SAFE_LEVELS:
        raise AssertionError(
            "Production must suppress normal EF SQL command text by setting "
            "Microsoft.EntityFrameworkCore.Database.Command to Warning, Error, Critical, or None."
        )

    ef_infrastructure_level = log_levels.get("Microsoft.EntityFrameworkCore.Infrastructure")
    if ef_infrastructure_level not in SAFE_LEVELS:
        raise AssertionError(
            "Production must keep Microsoft.EntityFrameworkCore.Infrastructure at Warning or higher "
            "unless a reviewed incident/debug override is used outside tracked config."
        )

    http_client_level = log_levels.get("System.Net.Http.HttpClient")
    if http_client_level is not None and http_client_level not in SAFE_LEVELS:
        raise AssertionError(
            "Production System.Net.Http.HttpClient logging, when configured, must be Warning, Error, Critical, or None."
        )

    print("Production logging config policy checks passed.")


if __name__ == "__main__":
    main()
