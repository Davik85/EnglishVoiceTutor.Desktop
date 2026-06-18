"""Static policy checks for provider billing entitlement stacking."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACTIVATION_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Billing" / "BillingEventEntitlementActivationService.cs"
SUBSCRIPTION_STATUS_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Subscriptions" / "SubscriptionStatusService.cs"
ADMIN_LOOKUP_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Admin" / "AdminUserLookupService.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    activation = read(ACTIVATION_SERVICE)
    status = read(SUBSCRIPTION_STATUS_SERVICE)
    admin = read(ADMIN_LOOKUP_SERVICE)

    assert_contains(
        activation,
        "CalculateStackedProviderEntitlementScheduleAsync",
        "deterministic stacked provider entitlement schedule helper",
    )
    assert_contains(
        activation,
        "providerPaidDuration = billingPeriodEndsAtUtc - providerPaidPeriodStartsAtUtc",
        "paid duration calculation from provider billing period",
    )
    assert_contains(
        activation,
        "stackStartsAtUtc.Add(providerPaidDuration)",
        "paid duration preservation when access is delayed",
    )
    assert_contains(
        activation,
        "dbContext.TrialGrants",
        "active trial_grants consideration",
    )
    assert_contains(
        activation,
        "trial.GrantedAtUtc <= nowUtc",
        "trial grant must already be active before stacking",
    )
    assert_contains(
        activation,
        "trial.ExpiresAtUtc > nowUtc",
        "trial grant must still be active before stacking",
    )
    assert_contains(
        activation,
        "FindCurrentOrScheduledProviderEventEntitlementAsync",
        "current or future provider_event lookup to avoid overlapping provider entitlements",
    )
    assert_contains(
        activation,
        "existingProviderEventEntitlement.ExpiresAtUtc.Value",
        "provider extension starts after existing provider entitlement expiry",
    )
    assert_not_contains(
        activation,
        "StartsAtUtc = validation.BillingPeriodStartsAtUtc ?? nowUtc",
        "direct provider entitlement start from billing period without stacking",
    )
    assert_contains(
        status,
        "entitlement.StartsAtUtc <= now",
        "subscription status only counts currently started Premium entitlement",
    )
    assert_contains(
        status,
        "!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc > now",
        "subscription status excludes expired Premium entitlement",
    )
    assert_contains(
        admin,
        "subscriptionStatusService.GetStatusAsync",
        "admin lookup uses subscription status service for premiumActive",
    )
    assert_contains(
        admin,
        "entitlement.StartsAtUtc <= now",
        "admin active entitlement list excludes future-start entitlements",
    )

    print("Billing entitlement stacking policy checks passed.")


if __name__ == "__main__":
    main()
