#!/usr/bin/env python3
"""Static policy checks for learner Premium continuous coverage display."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Subscriptions" / "SubscriptionStatusService.cs"
DTO = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Contracts" / "Subscription" / "SubscriptionStatusResponse.cs"
MODEL = ROOT / "Models" / "BackendSubscriptionStatusResponse.cs"
VM = ROOT / "ViewModels" / "SettingsViewModel.cs"


def need(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def main() -> None:
    service = SERVICE.read_text(encoding="utf-8")
    dto = DTO.read_text(encoding="utf-8")
    model = MODEL.read_text(encoding="utf-8")
    vm = VM.read_text(encoding="utf-8")

    for field in ["PremiumCoverageStartsAtUtc", "PremiumCoverageEndsAtUtc", "PremiumCoverageDisplayStatusCode"]:
        need(dto, field, f"backend learner coverage DTO field {field}")
        need(model, field, f"desktop learner coverage model field {field}")

    need(service, "CalculateContinuousPremiumCoverage", "backend-owned continuous coverage calculation")
    need(service, "IReadOnlyList<EntitlementEntity> futurePremiumEntitlements", "all future Premium entitlements supplied to coverage calculation")
    need(service, "SourceProviderEvent", "coverage extends only through provider-event future entitlements")
    need(service, "if (entitlement.StartsAtUtc > coverageEndsAtUtc.Value)", "gap stops continuous coverage chain")
    need(service, "coverageEndsAtUtc = entitlement.ExpiresAtUtc.Value", "continuous queued entitlements extend coverage end")
    need(service, "if (!entitlement.ExpiresAtUtc.HasValue)", "indefinite entitlement handling")
    need(service, "response.PremiumCoverageEndsAtUtc = coverage.EndsAtUtc", "learner summary emits coverage end")
    need(service, "entitlement.StartsAtUtc > now", "future entitlements are queried separately from active access")
    need(service, "entitlement.StartsAtUtc <= now", "PremiumActive remains based on currently started entitlement")

    need(vm, "status.PremiumCoverageDisplayStatusCode", "desktop uses backend coverage status code")
    need(vm, "status.PremiumCoverageEndsAtUtc", "desktop uses backend coverage end")
    need(vm, "status.PremiumCoverageStartsAtUtc", "desktop uses backend coverage start")

    print("Learner Premium continuous coverage policy passed.")


if __name__ == "__main__":
    main()
