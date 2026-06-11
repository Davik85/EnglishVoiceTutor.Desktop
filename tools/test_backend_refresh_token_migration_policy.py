#!/usr/bin/env python3
"""Policy checks for the refresh-token EF Core migration metadata."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
MIGRATIONS_DIR = ROOT / "backend/EnglishVoiceTutor.Api/Migrations"
MIGRATION_ID = "20260611000000_AddUserRefreshTokens"
MIGRATION_FILE = MIGRATIONS_DIR / f"{MIGRATION_ID}.cs"
DESIGNER_FILE = MIGRATIONS_DIR / f"{MIGRATION_ID}.Designer.cs"
ENTITY_FILE = ROOT / "backend/EnglishVoiceTutor.Api/Data/Entities/UserRefreshTokenEntity.cs"


def read(path: pathlib.Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.S):
        raise AssertionError(f"Missing {label}: {pattern}")


def main() -> int:
    migration = read(MIGRATION_FILE)
    designer = read(DESIGNER_FILE)
    entity = read(ENTITY_FILE)

    assert_contains(designer, "[DbContext(typeof(AppDbContext))]", "refresh-token migration DbContext metadata")
    assert_contains(designer, f'[Migration("{MIGRATION_ID}")]', "refresh-token migration id metadata")
    assert_not_contains(migration, f'[Migration("{MIGRATION_ID}")]', "duplicate migration id attribute outside designer metadata")
    assert_contains(designer, "partial class AddUserRefreshTokens", "refresh-token designer partial class")
    assert_contains(designer, "void BuildTargetModel(ModelBuilder modelBuilder)", "refresh-token designer target model")
    assert_contains(designer, 'b.ToTable("user_refresh_tokens", (string)null);', "refresh-token target table metadata")
    assert_contains(designer, 'b.Property<string>("TokenHash")', "refresh-token target model hash property")

    assert_regex(
        migration,
        r'CreateTable\(\s*name:\s*"user_refresh_tokens"',
        "refresh-token migration creates user_refresh_tokens",
    )
    assert_regex(
        migration,
        r'CreateIndex\(\s*name:\s*"IX_user_refresh_tokens_TokenHash",\s*table:\s*"user_refresh_tokens",\s*column:\s*"TokenHash",\s*unique:\s*true\s*\)',
        "refresh-token migration creates unique TokenHash index",
    )
    assert_contains(migration, "TokenHash", "refresh-token migration stores only token hashes")
    assert_not_contains(migration, "RefreshToken =", "plaintext refresh-token assignment in migration")
    assert_not_contains(migration, 'name: "RefreshToken"', "plaintext refresh-token column in migration")
    assert_not_contains(migration, '"refresh_token"', "plaintext refresh-token column in migration")

    assert_contains(entity, "TokenHash", "hashed token storage field")
    assert_not_contains(entity, "public string RefreshToken", "plaintext refresh-token entity property")
    assert_not_contains(entity, "public string? RefreshToken", "nullable plaintext refresh-token entity property")

    migration_sources = [
        path
        for path in MIGRATIONS_DIR.glob("*.cs")
        if not path.name.endswith(".Designer.cs") and path.name != "AppDbContextModelSnapshot.cs"
    ]
    refresh_token_migrations = [
        path.name
        for path in migration_sources
        if "user_refresh_tokens" in read(path)
    ]
    if refresh_token_migrations != [f"{MIGRATION_ID}.cs"]:
        raise AssertionError(
            "Refresh-token migration must not be duplicated under another timestamp/name; "
            f"found: {refresh_token_migrations}"
        )

    matching_names = sorted(path.name for path in MIGRATIONS_DIR.glob("*AddUserRefreshTokens*"))
    expected_names = [f"{MIGRATION_ID}.Designer.cs", f"{MIGRATION_ID}.cs"]
    if matching_names != expected_names:
        raise AssertionError(
            "Refresh-token migration id/name must not be duplicated or renamed; "
            f"expected {expected_names}, found {matching_names}"
        )

    print("Backend refresh-token migration policy checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
