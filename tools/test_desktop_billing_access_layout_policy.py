#!/usr/bin/env python3
"""Static policy checks for the simplified learner billing summary."""
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

    for field in ["CurrentTariffId", "CurrentTariffName", "CurrentTariffDisplayCode", "FreeLessonsRemainingDisplayCode", "PremiumDisplayStatusCode", "AutoRenewalStatusCode"]:
        need(dto, field, f"backend DTO {field}")
        need(model, field, f"desktop model {field}")

    need(service, 'response.CurrentTariffId = SubscriptionConstants.Plans.TrialPlanId', "trial tariff summary")
    need(service, 'response.FreeLessonsRemainingDisplayCode = "unlimited"', "unlimited lessons for entitlement tariffs")
    need(service, 'response.AutoRenewalStatusCode = response.RenewalStatus == SubscriptionConstants.RenewalStatuses.RenewalActive', "learner auto-renewal summary")
    need(service, 'if (trialActive)', "trial takes tariff priority over paid provider future/current details")

    need(vm, 'LocalizeUiText("Current tariff: {0}")', "current tariff primary line")
    need(vm, 'LocalizeUiText("Free lessons remaining: {0}")', "free lessons summary line")
    need(vm, 'LocalizeUiText("Premium: {0}")', "premium summary line")
    need(vm, 'LocalizeUiText("Auto-renewal: {0}")', "auto-renewal summary line")
    need(vm, '"trial" => LocalizeUiText("Trial")', "trial tariff label")
    need(vm, 'return LocalizeUiText("without limits");', "trial/premium unlimited label")
    need(vm, 'CancelSubscriptionNoticeText = string.Empty;', "no learner cancellation diagnostic notice")
    forbid(vm, 'LocalizeUiText("Current access: {0}")', "old current access learner line")
    for technical in ["BuildRenewalStatusText", "BuildNextRenewalText", "BuildCancellationStatusText", "BuildPaidAccessText"]:
        forbid(vm, technical, f"technical learner renderer {technical}")

    for forbidden_binding in ["SubscriptionTrialText", "SubscriptionNextRenewalText", "SubscriptionPaidAccessUntilText", "SubscriptionCancellationStatusText", "SubscriptionEnforcementText", "SubscriptionSourceText", "SubscriptionCheckedAtText"]:
        forbid(xaml, f'Text="{{Binding {forbidden_binding}}}"', f"technical learner binding {forbidden_binding}")
    for technical in ["renewal_expected", "nextRenewalState", "cancellationExplanationCode", "providerSubscriptionPresent", "scheduledChangeAction"]:
        forbid(xaml, technical, f"technical token in learner XAML: {technical}")

    need(xaml, '<WrapPanel Margin="0,12,0,0" Orientation="Horizontal">', "billing button wrap panel")
    need(xaml, 'TextTrimming="None"', "non-trimming billing text")
    print("Desktop learner billing summary policy passed.")

if __name__ == "__main__":
    main()
