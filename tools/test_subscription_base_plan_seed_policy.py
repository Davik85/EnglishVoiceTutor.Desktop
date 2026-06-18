"""Static policy checks for required base subscription plan seed data."""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
MIGRATIONS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Migrations"
CONSTANTS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Constants" / "SubscriptionConstants.cs"
APPSETTINGS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "appsettings.json"
DOCS_AND_CODE = [
    ROOT / "backend" / "EnglishVoiceTutor.Api",
    ROOT / "docs",
    ROOT / "tools",
]
GENERATED_FORBIDDEN = [
    ROOT / "artifacts",
    ROOT / "bin",
    ROOT / "obj",
]
SECRET_PATTERNS = [
    re.compile(r"pdl_(?:live|sandbox)_[A-Za-z0-9_\-]{12,}"),
    re.compile(r"sk_(?:live|test)_[A-Za-z0-9_\-]{12,}"),
]


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def all_text_files(root: Path):
    if not root.exists():
        return
    for path in root.rglob("*"):
        if path.is_file() and path.suffix.lower() not in {".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".exe", ".dll", ".pdb"}:
            yield path


def git_tracked_paths_under(paths: list[Path]) -> list[str]:
    relative_paths = [path.relative_to(ROOT).as_posix() for path in paths]
    result = subprocess.run(
        ["git", "ls-files", "--", *relative_paths],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return [line for line in result.stdout.splitlines() if line]


def main() -> None:
    constants = read(CONSTANTS)
    assert_contains(constants, 'FreePlanId = "free"', "free plan constant")
    assert_contains(constants, 'PremiumPlanId = "premium"', "premium plan constant")

    migration_files = sorted(MIGRATIONS.glob("*.cs"))
    seed_migrations = [path for path in migration_files if "plan" in read(path).lower() and "on conflict" in read(path).lower()]
    if not seed_migrations:
        raise AssertionError("Missing EF migration or approved seeding mechanism with idempotent plan upsert.")

    combined_seed = "\n".join(read(path) for path in seed_migrations)
    assert_contains(combined_seed, "SubscriptionConstants.Plans.FreePlanId", "free plan seed constant usage")
    assert_contains(combined_seed, "SubscriptionConstants.Plans.PremiumPlanId", "premium plan seed constant usage")
    assert_contains(combined_seed, "ON CONFLICT", "PostgreSQL idempotent upsert")
    assert_contains(combined_seed, '"PlanId"', "PlanId conflict target")
    assert_contains(combined_seed, "TRUE", "active plan seed values")
    assert_contains(combined_seed, '"IsActive" = TRUE', "existing plan reactivation")

    default_config = read(APPSETTINGS)
    assert_contains(default_config, '"CheckoutEnabled": false', "billing checkout disabled by default")
    assert_contains(default_config, '"Provider": "none"', "billing provider disabled by default")
    assert_contains(default_config, '"CheckoutAdapterEnabled": false', "Paddle checkout adapter disabled by default")
    assert_contains(default_config, '"Environment": "sandbox"', "Paddle sandbox default")
    assert_contains(default_config, '"ApiKey": ""', "no Paddle API key in default config")
    assert_contains(default_config, '"PremiumPriceId": ""', "no Paddle price id in default config")
    assert_contains(default_config, '"ClientSideToken": ""', "no Paddle client token in default config")

    tracked_generated = git_tracked_paths_under(GENERATED_FORBIDDEN)
    if tracked_generated:
        formatted_paths = "\n".join(f"- {path}" for path in tracked_generated)
        raise AssertionError(f"Generated artifact paths must not be tracked by git:\n{formatted_paths}")

    for root in DOCS_AND_CODE:
        for path in all_text_files(root):
            text = read(path)
            for pattern in SECRET_PATTERNS:
                if pattern.search(text):
                    raise AssertionError(f"Potential secret-like token found in tracked source: {path}")

    print("Subscription base plan seed policy checks passed.")


if __name__ == "__main__":
    main()
