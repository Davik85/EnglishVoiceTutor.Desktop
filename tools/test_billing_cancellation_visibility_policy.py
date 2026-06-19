from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]

def read(path): return (ROOT / path).read_text(encoding='utf-8')
def require(text, needle, label):
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")

def forbid(text, needle, label):
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")

status = read('backend/EnglishVoiceTutor.Api/Services/Subscriptions/SubscriptionStatusService.cs')
contract = read('backend/EnglishVoiceTutor.Api/Contracts/Subscription/SubscriptionStatusResponse.cs')
admin = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js')
model = read('Models/BackendSubscriptionStatusResponse.cs')
settings = read('ViewModels/SettingsViewModel.cs')
xaml = read('Views/SettingsView.xaml')
loc = read('Localization/AppLocalization.cs')

for needle in ['RenewalStatus', 'NextRenewalState', 'CanRequestCancelRenewal', 'CancellationExplanationCode', 'PaidAccessUntilUtc']:
    require(contract, needle, f'backend DTO {needle}')
    require(model, needle, f'desktop DTO {needle}')

for needle in [
    'SubscriptionConstants.RenewalStatuses.RenewalActive',
    'response.CanRequestCancelRenewal = true',
    'SubscriptionConstants.RenewalStatuses.CancellationScheduled',
    'SubscriptionConstants.RenewalStatuses.NoPaidSubscription',
    'SubscriptionConstants.RenewalStatuses.SubscriptionCanceled',
    'SubscriptionConstants.CancellationExplanationCodes.AlreadyScheduled',
]:
    require(status, needle, f'backend mapping policy {needle}')

for needle in ['renewalStatus', 'nextRenewalState', 'cancelAtPeriodEnd', 'scheduledChangeEffectiveAtUtc', 'currentPeriodEndUtc', 'paidAccessUntilUtc', 'canRequestCancelRenewal', 'cancellationExplanationCode', 'providerSubscriptionPresent']:
    require(admin, f'"{needle}"', f'admin diagnostic field {needle}')

for needle in ['status.CanRequestCancelRenewal ??', 'BuildRenewalStatusText', 'BuildNextRenewalText', 'BuildCancellationStatusText', 'lastKnownRenewalStatus == "cancellation_scheduled"']:
    require(settings, needle, f'desktop cancellation state usage {needle}')
for needle in ['SubscriptionRenewalText', 'SubscriptionNextRenewalText', 'SubscriptionPaidAccessUntilText', 'SubscriptionCancellationStatusText']:
    require(xaml, needle, f'desktop account UI binding {needle}')

for needle in ['Paddle.Api', 'api.paddle.com', 'Paddle-Signature', 'ApiKey', 'webhook secret', 'ProviderSubscriptionId']:
    forbid(settings, needle, 'desktop provider secret/API detail')
    forbid(xaml, needle, 'desktop provider secret/API detail')

supported = ['en','es','fr','de','it','pt','ru','pl','ar','ja','ko','sr','hr','bg']
keys = ['Renewal: active','Renewal: cancellation scheduled','Renewal: no paid subscription','Next renewal: no further renewal scheduled','Cancellation status: already scheduled','Cancellation status: not available for trial/manual Premium']
for lang in supported:
    block_start = loc.find(f'["{lang}"] = new(StringComparer.OrdinalIgnoreCase)')
    if block_start < 0: raise AssertionError(f'Missing localization block {lang}')
    block_end = loc.find('\n            },', block_start)
    block = loc[block_start:block_end]
    for key in keys:
        require(block, f'["{key}"]', f'{lang} localization for {key}')
ru_block = loc[loc.find('["ru"] = new(StringComparer.OrdinalIgnoreCase)'):loc.find('\n            },', loc.find('["ru"] = new(StringComparer.OrdinalIgnoreCase)'))]
pl_block = loc[loc.find('["pl"] = new(StringComparer.OrdinalIgnoreCase)'):loc.find('\n            },', loc.find('["pl"] = new(StringComparer.OrdinalIgnoreCase)'))]
require(ru_block, 'Продление: активно', 'Russian renewal translation')
require(pl_block, 'Odnowienie: aktywne', 'Polish renewal translation')
print('Billing cancellation visibility policy checks passed.')
