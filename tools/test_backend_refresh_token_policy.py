from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    return (ROOT / path).read_text(encoding='utf-8')

def assert_contains(text, needle, label):
    if needle not in text:
        raise SystemExit(f"ERROR: Missing {label}: {needle}")

contract = read('backend/EnglishVoiceTutor.Api/Contracts/Auth/AuthResponse.cs')
endpoints = read('backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs')
service = read('backend/EnglishVoiceTutor.Api/Services/Auth/AuthService.cs')
hasher = read('backend/EnglishVoiceTutor.Api/Services/Auth/RefreshTokenHasher.cs')
entity = read('backend/EnglishVoiceTutor.Api/Data/Entities/UserRefreshTokenEntity.cs')
migration = read('backend/EnglishVoiceTutor.Api/Migrations/20260611000000_AddUserRefreshTokens.cs')
options = read('backend/EnglishVoiceTutor.Api/Options/JwtOptions.cs')

assert_contains(contract, 'RefreshToken', 'refresh token response field')
assert_contains(contract, 'RefreshTokenExpiresAtUtc', 'refresh token expiry response field')
assert_contains(endpoints, 'ApiConstants.AuthRefreshRoute', 'refresh endpoint mapping')
assert_contains(endpoints, 'ApiConstants.AuthRevokeRoute', 'revoke endpoint mapping')
assert_contains(hasher, 'RandomNumberGenerator', 'cryptographically secure token generation')
assert_contains(hasher, 'SHA256.HashData', 'refresh token hashing')
assert_contains(entity, 'TokenHash', 'hashed token storage field')
assert_contains(service, 'ReplacedByTokenHash', 'refresh token rotation replacement marker')
assert_contains(service, 'refresh_token_reuse', 'reuse detection revocation reason')
assert_contains(options, 'RefreshTokenLifetimeDays', 'finite refresh token lifetime configuration')
assert_contains(migration, 'user_refresh_tokens', 'refresh-token migration table')
assert_contains(migration, 'TokenHash', 'migration stores token hash only')
if 'RefreshToken =' in migration or 'refresh_token"' in migration:
    raise SystemExit('ERROR: Migration appears to store plaintext refresh token.')
print('Backend refresh token policy checks passed.')
