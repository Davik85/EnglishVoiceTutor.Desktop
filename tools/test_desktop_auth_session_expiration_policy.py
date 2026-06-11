#!/usr/bin/env python3
"""Policy checks for desktop refresh-token-aware auth session expiration."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding='utf-8')

def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f'Missing {label}: {needle}')

def main() -> None:
    storage = read('Services/Auth/AuthSessionStorageService.cs')
    auth_backend = read('Services/Auth/AuthBackendService.cs')
    stored_session = read('Models/Auth/StoredAuthSession.cs')
    desktop_auth_response = read('Models/Auth/AuthResponse.cs')
    backend_auth_response = read('backend/EnglishVoiceTutor.Api/Contracts/Auth/AuthResponse.cs')
    jwt_options = read('backend/EnglishVoiceTutor.Api/Options/JwtOptions.cs')
    jwt_service = read('backend/EnglishVoiceTutor.Api/Services/Auth/JwtTokenService.cs')
    program = read('backend/EnglishVoiceTutor.Api/Program.cs')
    auth_endpoints = read('backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs')
    backend_constants = read('Constants/BackendConstants.cs')

    assert_contains(jwt_options, 'public int AccessTokenLifetimeMinutes { get; set; } = 60;', 'default access-token lifetime')
    assert_contains(jwt_options, 'RefreshTokenLifetimeDays', 'finite refresh-token lifetime')
    assert_contains(jwt_service, 'now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes)', 'JWT expiration uses configured lifetime')
    assert_contains(jwt_service, 'expires: expiresAt.UtcDateTime', 'JWT exp claim is emitted')
    assert_contains(program, 'ValidateLifetime = true', 'JWT lifetime validation is enabled')
    assert_contains(program, 'ClockSkew = TimeSpan.Zero', 'strict JWT expiration')
    assert_contains(auth_endpoints, 'MapPost(ApiConstants.AuthRefreshRoute, RefreshAsync);', 'refresh endpoint exists')
    assert_contains(auth_endpoints, 'MapGet(ApiConstants.AuthMeRoute, GetMeAsync).RequireAuthorization();', 'me endpoint still requires access token')

    for text, label in [(stored_session, 'desktop stored session'), (desktop_auth_response, 'desktop response'), (backend_auth_response, 'backend response')]:
        assert_contains(text, 'RefreshToken', label)
        assert_contains(text, 'RefreshTokenExpiresAtUtc', label)

    assert_contains(storage, 'IsRefreshTokenExpired', 'desktop clears only expired refresh sessions')
    assert_contains(storage, 'ShouldRefreshAccessToken', 'desktop refreshes access token before expiry')
    assert_contains(storage, 'session.RefreshTokenExpiresAtUtc > DateTimeOffset.UtcNow', 'access expiry alone does not invalidate refreshable sessions')
    assert_contains(auth_backend, 'EnsureAuthenticatedSessionAsync', 'central session restore and refresh')
    assert_contains(auth_backend, 'AuthSessionEnsureStatus.TemporarilyUnavailable', 'temporary refresh failure preserves session')
    assert_contains(auth_backend, 'await sessionStorageService.ClearAsync(cancellationToken);', 'confirmed invalid refresh clears saved session')
    assert_contains(backend_constants, 'AuthRefreshEndpoint', 'desktop knows refresh endpoint')
    print('Desktop auth session expiration policy checks passed.')

if __name__ == '__main__':
    main()
