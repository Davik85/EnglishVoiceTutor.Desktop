from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    return (ROOT / path).read_text(encoding='utf-8')

def assert_contains(text, needle, label):
    if needle not in text:
        raise SystemExit(f"ERROR: Missing {label}: {needle}")

stored = read('Models/Auth/StoredAuthSession.cs')
response = read('Models/Auth/AuthResponse.cs')
storage = read('Services/Auth/AuthSessionStorageService.cs')
auth = read('Services/Auth/AuthBackendService.cs')
helper = read('Services/Auth/AuthenticatedRequestHelper.cs')
history_client = read('Services/BackendLessonHistoryClient.cs')
constants = read('Constants/BackendConstants.cs')
settings = read('ViewModels/SettingsViewModel.cs')
backend_constants = read('Constants/BackendConstants.cs')

for text, label in [(stored, 'stored session'), (response, 'auth response')]:
    assert_contains(text, 'RefreshToken', label + ' refresh token')
    assert_contains(text, 'RefreshTokenExpiresAtUtc', label + ' refresh token expiry')
assert_contains(storage, 'IsRefreshTokenExpired', 'refresh expiry check')
assert_contains(storage, 'ShouldRefreshAccessToken', 'proactive refresh window')
assert_contains(auth, 'EnsureAuthenticatedSessionAsync', 'central refresh-aware session retrieval')
assert_contains(auth, 'RefreshAuthenticatedSessionOnceAsync', '401 refresh retry support')
assert_contains(auth, 'AuthSessionEnsureStatus.TemporarilyUnavailable', 'temporary backend unavailable state')
assert_contains(auth, 'RevokeRefreshTokenBestEffortAsync', 'logout revoke behavior')
assert_contains(helper, 'SendWithRefreshRetryAsync', 'central 401 retry helper')
assert_contains(history_client, 'SendWithRefreshRetryAsync', 'authenticated API call uses central 401 retry helper')
assert_contains(constants, 'AuthRefreshEndpoint = "/api/auth/refresh"', 'desktop refresh endpoint')
assert_contains(backend_constants, 'https://api.languagevoicetutor.com', 'release backend lock')
if 'Diagnostics' in settings and 'SettingsTab.Diagnostics' in settings:
    raise SystemExit('ERROR: Release Settings appears to expose Diagnostics tab.')
if 'Backend URL' in settings and '#if DEBUG' not in settings:
    raise SystemExit('ERROR: Release Settings appears to expose Backend URL field outside DEBUG guard.')
print('Desktop refresh token session policy checks passed.')
