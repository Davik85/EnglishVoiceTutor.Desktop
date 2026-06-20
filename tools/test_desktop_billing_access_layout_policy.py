#!/usr/bin/env python3
"""Static policy checks for Desktop billing access rendering and localized button layout."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VM = ROOT / "ViewModels" / "SettingsViewModel.cs"
XAML = ROOT / "Views" / "SettingsView.xaml"
MODEL = ROOT / "Models" / "BackendSubscriptionStatusResponse.cs"
DTO = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Contracts" / "Subscription" / "SubscriptionStatusResponse.cs"
SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Subscriptions" / "SubscriptionStatusService.cs"


def need(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def main() -> None:
    vm = VM.read_text(encoding="utf-8")
    xaml = XAML.read_text(encoding="utf-8")
    model = MODEL.read_text(encoding="utf-8")
    dto = DTO.read_text(encoding="utf-8")
    service = SERVICE.read_text(encoding="utf-8")

    for field in ["CurrentAccessTier", "DailyFreeLimitApplies", "HasScheduledPaidPremium", "ScheduledPaidPremiumStartUtc"]:
        need(dto, field, f"backend DTO {field}")
        need(model, field, f"desktop model {field}")

    need(service, 'response.CurrentAccessTier = "trial_premium"', "trial Premium current access")
    need(service, 'response.DailyFreeLimitApplies = false', "Premium disables daily free limit")
    need(service, 'response.HasScheduledPaidPremium = true', "future provider_event scheduled paid Premium")
    need(service, 'entitlement.StartsAtUtc <= now', "future paid Premium is not active")

    need(vm, 'LocalizeUiText("Current access: {0}")', "current access primary line")
    need(vm, '"current_access_trial_premium" => LocalizeUiText("Trial Premium")', "trial Premium label")
    need(vm, 'LocalizeUiText("Free lessons: no daily limit")', "unlimited free lesson label")
    need(vm, 'status.DailyFreeLimitApplies ?? !status.PremiumActive', "backend-driven daily limit flag")
    need(vm, 'LocalizeUiText("Paid Premium starts: {0}")', "scheduled paid start line")
    need(vm, 'LocalizeUiText("Paid Premium access until: {0}")', "paid Premium until line")
    forbid(vm, 'SubscriptionPlanText = $"{localizedText.SubscriptionPlanLabel}', "old Plan primary rendering")

    need(xaml, '<WrapPanel Margin="0,12,0,0" Orientation="Horizontal">', "billing button wrap panel")
    forbid(xaml, 'MaxWidth="260"', "fixed clipping max width on billing buttons")
    forbid(xaml, 'MaxWidth="300"', "fixed clipping max width on cancel button")
    need(xaml, 'TextTrimming="None"', "non-trimming billing button content")

    need(vm, 'SizeToContent = SizeToContent.WidthAndHeight', "confirmation dialog sizes to content")
    need(vm, 'new WrapPanel { Orientation = Orientation.Horizontal', "confirmation buttons wrap")
    need(vm, 'TextWrapping = TextWrapping.Wrap', "confirmation button wrapping")
    need(vm, 'MinWidth = 156', "minimum tappable confirmation button width")
    forbid(vm, 'Width = 420', "old fixed narrow dialog width")
    print("Desktop billing access/layout policy passed.")

if __name__ == "__main__":
    main()
