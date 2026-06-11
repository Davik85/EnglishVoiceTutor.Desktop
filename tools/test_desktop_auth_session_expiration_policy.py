#!/usr/bin/env python3
"""Policy checks for desktop auth-session expiration and invalidation behavior."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROD_URL = "https://api.languagevoicetutor.com"


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.exists():
        raise AssertionError(f"Missing required file: {relative}")
    return path.read_text(encoding="utf-8")


def assert_contains(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def assert_not_contains(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


def assert_regex(text: str, pattern: str, label: str) -> None:
    if not re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Missing {label}: {pattern}")


def assert_not_regex(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, re.IGNORECASE | re.DOTALL):
        raise AssertionError(f"Forbidden {label}: {pattern}")


def method_body(text: str, signature: str, label: str) -> str:
    start = text.find(signature)
    if start == -1:
        raise AssertionError(f"Missing method for {label}: {signature}")

    brace = text.find("{", start)
    if brace == -1:
        raise AssertionError(f"Missing method body for {label}: {signature}")

    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start : index + 1]

    raise AssertionError(f"Unterminated method body for {label}: {signature}")


def main() -> None:
    storage = read("Services/Auth/AuthSessionStorageService.cs")
    auth_backend = read("Services/Auth/AuthBackendService.cs")
    stored_session = read("Models/Auth/StoredAuthSession.cs")
    desktop_auth_response = read("Models/Auth/AuthResponse.cs")
    backend_auth_response = read("backend/EnglishVoiceTutor.Api/Contracts/Auth/AuthResponse.cs")
    jwt_options = read("backend/EnglishVoiceTutor.Api/Options/JwtOptions.cs")
    jwt_service = read("backend/EnglishVoiceTutor.Api/Services/Auth/JwtTokenService.cs")
    program = read("backend/EnglishVoiceTutor.Api/Program.cs")
    backend_appsettings = read("backend/EnglishVoiceTutor.Api/appsettings.json")
    backend_appsettings_dev = read("backend/EnglishVoiceTutor.Api/appsettings.Development.json")
    auth_endpoints = read("backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs")
    main_vm = read("ViewModels/MainViewModel.cs")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_xaml = read("Views/SettingsView.xaml")
    settings_code = read("Views/SettingsView.xaml.cs")
    backend_constants = read("Constants/BackendConstants.cs")
    backend_builder = read("Services/BackendEndpointBuilder.cs")
    project = read("EnglishVoiceTutor.Desktop.csproj")

    # Backend JWT lifetime is explicit, discoverable, and enforced by ASP.NET auth.
    assert_contains(jwt_options, "public int AccessTokenLifetimeMinutes { get; set; } = 60;", "default access-token lifetime")
    assert_contains(backend_appsettings, '"AccessTokenLifetimeMinutes": 60', "production-configured access-token lifetime")
    assert_contains(backend_appsettings_dev, '"AccessTokenLifetimeMinutes": 60', "development-configured access-token lifetime")
    assert_contains(jwt_service, "now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes)", "JWT expiration uses configured lifetime")
    assert_contains(jwt_service, "expires: expiresAt.UtcDateTime", "JWT exp claim is emitted")
    assert_contains(backend_auth_response, "DateTimeOffset ExpiresAtUtc", "auth response exposes expiration timestamp")
    assert_contains(program, "ValidateLifetime = true", "JWT lifetime validation is enabled")
    assert_contains(program, "ClockSkew = TimeSpan.Zero", "JWT expiration has no hidden clock-skew extension")
    assert_contains(auth_endpoints, "MapGet(ApiConstants.AuthMeRoute, GetMeAsync).RequireAuthorization();", "account-status endpoint requires valid token")

    # No refresh-token contract or desktop refresh path exists yet.
    combined_auth_contract = "\n".join([stored_session, desktop_auth_response, backend_auth_response, auth_backend, auth_endpoints])
    assert_not_regex(combined_auth_contract, r"refresh\s*token|refreshToken|RefreshToken", "refresh-token implementation")
    assert_contains(stored_session, "DateTimeOffset ExpiresAtUtc", "desktop stores explicit expiration timestamp")
    assert_contains(auth_backend, "ExpiresAtUtc = payload.ExpiresAtUtc", "desktop persists backend expiration timestamp")

    # Local restore rejects expired sessions, but generic load/DPAPI failures must not be confused with backend rejection.
    assert_regex(storage, r"IsExpired\(StoredAuthSession session\).*ExpiresAtUtc\s*<=\s*DateTimeOffset\.UtcNow", "local expiration check")
    assert_regex(storage, r"GetValidSessionOrNullAsync\(.*?if \(IsExpired\(session\)\).*?await ClearAsync\(cancellationToken\);", "expired local session cleanup")
    assert_regex(storage, r"TryLoadSessionFileAsync\(.*?catch\s*\{\s*TryDeleteSessionFile\(sessionFilePath\);", "corrupt unreadable session cleanup")

    get_me = method_body(auth_backend, "public async Task<AuthMeResult> GetMeAsync", "auth /me validation")
    assert_contains(get_me, "response.StatusCode == HttpStatusCode.Unauthorized", "confirmed invalid/expired token detection")
    assert_contains(get_me, "await sessionStorageService.ClearAsync(cancellationToken);", "clear saved session on confirmed unauthorized token")
    assert_contains(get_me, "NotifyAuthStateChanged(null);", "notify sign-out on confirmed unauthorized token")
    assert_contains(get_me, "return AuthMeResult.InvalidSession();", "invalid-session result for confirmed unauthorized token")
    assert_contains(get_me, "if (!response.IsSuccessStatusCode)", "non-success handling")
    assert_contains(get_me, "return AuthMeResult.BackendUnavailable();", "backend unavailable result")

    unavailable_section = get_me.split("if (!response.IsSuccessStatusCode)", 1)[1]
    unavailable_section = unavailable_section.split("var user =", 1)[0]
    assert_not_contains(unavailable_section, "ClearAsync", "session clearing on non-401 backend responses")
    catch_section = get_me.split("catch (Exception exception)", 1)[1]
    assert_contains(catch_section, "return AuthMeResult.BackendUnavailable();", "network/timeouts preserve session as backend unavailable")
    assert_not_contains(catch_section, "ClearAsync", "session clearing on transient network/backend exception")

    startup_restore = method_body(main_vm, "private async Task TryRestoreSavedAuthSessionOnStartupAsync", "startup session restore")
    assert_contains(startup_restore, "TryRestoreSessionAsync", "startup restore uses stored session")
    assert_contains(startup_restore, "meResult.Status == AuthMeResultStatus.InvalidSession", "startup detects confirmed invalid session")
    assert_contains(startup_restore, "await authBackendService.LogoutAsync();", "startup clears confirmed invalid session")
    assert_contains(startup_restore, "meResult.Status == AuthMeResultStatus.BackendUnavailable", "startup distinguishes backend unavailable")
    backend_unavailable_section = startup_restore.split("meResult.Status == AuthMeResultStatus.BackendUnavailable", 1)[1]
    backend_unavailable_section = backend_unavailable_section.split("Debug.WriteLine(\"Saved auth session restored", 1)[0]
    assert_contains(backend_unavailable_section, "StoredSessionCleared=False", "startup logs preserved session on backend unavailable")
    assert_not_contains(backend_unavailable_section, "LogoutAsync", "startup does not logout on backend unavailable")
    catch_startup_section = startup_restore.split("catch (Exception exception)", 1)[1]
    assert_not_contains(catch_startup_section, "LogoutAsync", "startup generic failure does not logout")
    assert_not_contains(catch_startup_section, "ClearAsync", "startup generic failure does not clear storage")

    restore_session = method_body(settings_vm, "private async Task RestoreSessionAsync", "manual settings session restore")
    assert_contains(restore_session, "StatusMessage = BackendUxText.SessionExpired;", "tester-facing session expired text")
    assert_contains(restore_session, "StatusMessage = BackendUxText.CouldNotConnect;", "tester-facing backend/network unavailable text")
    assert_contains(restore_session, "meResult.Status == AuthMeResultStatus.InvalidSession", "manual restore distinguishes invalid session")
    assert_contains(restore_session, "meResult.Status == AuthMeResultStatus.BackendUnavailable", "manual restore distinguishes backend unavailable")

    # Release Settings and backend targeting remain locked down while auditing auth behavior.
    assert_not_contains(settings_xaml, "DiagnosticsSection", "release Settings Diagnostics tab")
    assert_not_contains(settings_xaml, "Backend URL", "release Settings Backend URL field")
    assert_contains(settings_code, "public static readonly bool DesktopDiagnosticsEnabled = false;", "release diagnostics disabled")
    assert_contains(backend_constants, f'ProductionBackendBaseUrl = "{PROD_URL}"', "production backend constant")
    assert_contains(backend_builder, "#else\n        return BackendConstants.ProductionBackendBaseUrl;", "release backend resolver ignores saved/dev URLs")
    assert_contains(project, f"'$(Configuration)' != 'Debug' and '$(DesktopBackendBaseUrl)' != '{PROD_URL}'", "release backend build lock")

    print("Desktop auth session expiration policy checks passed.")


if __name__ == "__main__":
    main()
