from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text, needle, label):
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


status = read("backend/EnglishVoiceTutor.Api/Services/Subscriptions/SubscriptionStatusService.cs")
contract = read("backend/EnglishVoiceTutor.Api/Contracts/Subscription/SubscriptionStatusResponse.cs")
admin = read("backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js")
model = read("Models/BackendSubscriptionStatusResponse.cs")
settings = read("ViewModels/SettingsViewModel.cs")
xaml = read("Views/SettingsView.xaml")
loc = read("Localization/AppLocalization.cs")

for needle in ["RenewalStatus", "NextRenewalState", "CanRequestCancelRenewal", "CancellationExplanationCode", "PaidAccessUntilUtc"]:
    require(contract, needle, f"backend DTO {needle}")
    require(model, needle, f"desktop DTO {needle}")

for needle in [
    "SubscriptionConstants.RenewalStatuses.RenewalActive",
    "response.CanRequestCancelRenewal = true",
    "SubscriptionConstants.RenewalStatuses.CancellationScheduled",
    "SubscriptionConstants.RenewalStatuses.NoPaidSubscription",
    "SubscriptionConstants.RenewalStatuses.SubscriptionCanceled",
    "SubscriptionConstants.CancellationExplanationCodes.AlreadyScheduled",
]:
    require(status, needle, f"backend mapping policy {needle}")

for needle in [
    "renewalStatus",
    "nextRenewalState",
    "cancelAtPeriodEnd",
    "scheduledChangeAction",
    "scheduledChangeEffectiveAtUtc",
    "canRequestCancelRenewal",
    "cancellationExplanationCode",
]:
    require(admin, f'"{needle}"', f"admin diagnostic field {needle}")

for needle in [
    'LocalizeUiText("Current tariff: {0}")',
    'LocalizeUiText("Free lessons remaining: {0}")',
    'LocalizeUiText("Premium: {0}")',
    'LocalizeUiText("Auto-renewal: {0}")',
    "BuildCurrentTariffLabel(status)",
    "BuildFreeLessonsRemainingLabel(status)",
    "BuildPremiumDisplayStatusLabel(status)",
    "BuildAutoRenewalStatusLabel(status)",
    "status.CanRequestCancelRenewal ??",
]:
    require(settings, needle, f"simplified desktop account summary or safe cancel visibility {needle}")

for needle in [
    "SubscriptionPlanText",
    "SubscriptionFreeLessonText",
    "SubscriptionPremiumText",
    "SubscriptionRenewalText",
]:
    require(xaml, needle, f"desktop account UI binding {needle}")

for needle in [
    "BuildRenewalStatusText",
    "BuildNextRenewalStateText",
    "BuildNextRenewalText",
    "BuildCancellationStatusText",
]:
    forbid(settings, needle, f"learner technical billing diagnostic renderer {needle}")

for needle in [
    "renewal_expected",
    "no_renewal_scheduled",
    "cancellation_scheduled",
    "cancellationExplanationCode",
    "providerSubscriptionPresent",
    "scheduledChangeAction",
    "SubscriptionNextRenewalText",
    "SubscriptionPaidAccessUntilText",
    "SubscriptionCancellationStatusText",
]:
    forbid(xaml, needle, f"learner technical billing diagnostic binding or literal {needle}")

for needle in ["Paddle.Api", "api.paddle.com", "Paddle-Signature", "ApiKey", "webhook secret", "ProviderSubscriptionId"]:
    forbid(settings, needle, "desktop provider secret/API detail")
    forbid(xaml, needle, "desktop provider secret/API detail")

supported = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
keys = ["Current tariff: {0}", "Free lessons remaining: {0}", "Premium: {0}", "Auto-renewal: {0}"]
for lang in supported:
    block_start = loc.find(f'["{lang}"] = new(StringComparer.OrdinalIgnoreCase)')
    if block_start < 0:
        raise AssertionError(f"Missing localization block {lang}")
    block_end = loc.find("\n            },", block_start)
    block = loc[block_start:block_end]
    for key in keys:
        require(block, f'["{key}"]', f"{lang} localization for {key}")

print("Billing cancellation visibility policy checks passed.")
