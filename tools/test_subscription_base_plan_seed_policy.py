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
SAFE_TEXT_EXTENSIONS = {
    ".cs", ".xaml", ".json", ".md", ".ps1", ".py", ".js", ".css", ".html",
    ".csproj", ".props", ".targets", ".yml", ".yaml", ".txt", ".config",
}
ALLOWED_BINARY_EXTENSIONS = {".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".exe", ".dll", ".pdb", ".snk"}
SKIPPED_PARTS = {"artifacts", "bin", "obj", "publish", "packages", ".git"}
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


def tracked_text_files_under(paths: list[Path]):
    for relative_path in git_tracked_paths_under(paths):
        path = ROOT / relative_path
        parts = set(path.relative_to(ROOT).parts)
        if parts & SKIPPED_PARTS:
            continue
        suffix = path.suffix.lower()
        if suffix in ALLOWED_BINARY_EXTENSIONS:
            continue
        if suffix not in SAFE_TEXT_EXTENSIONS:
            continue
        yield path


def read_tracked_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError as exc:
        relative_path = path.relative_to(ROOT).as_posix()
        raise AssertionError(f"Tracked text-like file is not valid UTF-8: {relative_path}") from exc


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
    assert_contains(constants, 'TrialPlanId = "trial"', "trial plan constant")

    migration_files = sorted(MIGRATIONS.glob("*.cs"))
    seed_migrations = [path for path in migration_files if "plan" in read(path).lower() and "on conflict" in read(path).lower()]
    if not seed_migrations:
        raise AssertionError("Missing EF migration or approved seeding mechanism with idempotent plan upsert.")

    combined_seed = "\n".join(read(path) for path in seed_migrations)
    assert_contains(combined_seed, "SubscriptionConstants.Plans.FreePlanId", "free plan seed constant usage")
    assert_contains(combined_seed, "SubscriptionConstants.Plans.PremiumPlanId", "premium plan seed constant usage")
    assert_contains(combined_seed, "SubscriptionConstants.Plans.TrialPlanId", "trial plan seed constant usage")
    assert_contains(combined_seed, "SubscriptionConstants.Plans.TrialPlanName", "trial plan seed display name")
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

    desktop_backend_constants = read(ROOT / "Constants" / "BackendConstants.cs")
    desktop_cancel_client = read(ROOT / "Services" / "BackendCancelSubscriptionClient.cs")
    backend_cancel_endpoint = read(ROOT / "backend" / "EnglishVoiceTutor.Api" / "Endpoints" / "BillingSubscriptionEndpoints.cs")
    backend_cancel_service = read(ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Billing" / "BillingSubscriptionCancellationService.cs")
    paddle_adapter = read(ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Billing" / "PaddleBillingProviderCheckoutAdapter.cs")
    assert_contains(desktop_backend_constants, 'MeBillingSubscriptionCancelEndpoint = "/api/me/billing/subscription/cancel"', "desktop backend cancel endpoint constant")
    assert_contains(desktop_cancel_client, "AuthenticatedRequestHelper.AddBearerTokenIfPresent", "desktop authenticated cancel request")
    assert_contains(backend_cancel_endpoint, ".RequireAuthorization()", "authenticated cancel endpoint")
    if "providerSubscriptionId" in backend_cancel_endpoint or "ProviderSubscriptionId" in backend_cancel_endpoint:
        raise AssertionError("Cancellation endpoint must not accept a provider subscription id from the client.")
    assert_contains(backend_cancel_service, "ProviderSubscriptionId = subscription.ProviderSubscriptionId", "backend-owned provider subscription id")
    if "dbContext.Entitlements" in backend_cancel_service:
        raise AssertionError("Cancellation request path must not modify or query entitlements.")
    assert_contains(paddle_adapter, 'effective_from = "next_billing_period"', "cancel-at-period-end Paddle request")

    tracked_generated = git_tracked_paths_under(GENERATED_FORBIDDEN)
    if tracked_generated:
        formatted_paths = "\n".join(f"- {path}" for path in tracked_generated)
        raise AssertionError(f"Generated artifact paths must not be tracked by git:\n{formatted_paths}")

    for path in tracked_text_files_under(DOCS_AND_CODE):
        text = read_tracked_text(path)
        for pattern in SECRET_PATTERNS:
            if pattern.search(text):
                raise AssertionError(f"Potential secret-like token found in tracked source: {path}")

    print("Subscription base plan seed policy checks passed.")


if __name__ == "__main__":
    main()
