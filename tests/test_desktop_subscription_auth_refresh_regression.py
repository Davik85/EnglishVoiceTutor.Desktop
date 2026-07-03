#!/usr/bin/env python3
"""Regression policy for desktop subscription status refresh-aware authentication."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SUBSCRIPTION_CLIENT = ROOT / "Services" / "BackendSubscriptionStatusClient.cs"
AUTHENTICATED_CLIENTS = [
    SUBSCRIPTION_CLIENT,
    ROOT / "Services" / "BackendCheckoutSessionClient.cs",
    ROOT / "Services" / "BackendCancelSubscriptionClient.cs",
    ROOT / "Services" / "BackendTrialClaimClient.cs",
    ROOT / "Services" / "BackendUserSettingsClient.cs",
    ROOT / "Services" / "BackendLessonAccessDecisionClient.cs",
]
AUTH_HELPER = ROOT / "Services" / "Auth" / "AuthenticatedRequestHelper.cs"
AUTH_BACKEND = ROOT / "Services" / "Auth" / "AuthBackendService.cs"


def read(path: pathlib.Path) -> str:
    if not path.exists():
        raise AssertionError(f"Missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def test_subscription_status_uses_refresh_aware_authenticated_flow() -> None:
    source = read(SUBSCRIPTION_CLIENT)

    assert "AuthBackendService authBackendService" in source
    assert "authBackendService.SetBackendBaseUrl(backendBaseUrl)" in source
    assert "AuthenticatedRequestHelper.SendWithRefreshRetryAsync" in source
    assert "BackendConstants.MeSubscriptionStatusEndpoint" in source
    assert "BackendConstants.DevSubscriptionStatusEndpoint" in source
    assert "GetValidSessionOrNullAsync" not in source
    assert "AddBearerTokenIfPresent" not in source
    assert not re.search(r"Headers\.Authorization\s*=", source)


def test_account_settings_premium_clients_do_not_bypass_refresh_aware_flow() -> None:
    for path in AUTHENTICATED_CLIENTS:
        source = read(path)
        relative_path = path.relative_to(ROOT)
        assert "AuthenticatedRequestHelper.SendWithRefreshRetryAsync" in source, relative_path
        assert "GetValidSessionOrNullAsync" not in source, relative_path
        assert "AddBearerTokenIfPresent" not in source, relative_path
        assert not re.search(r"Headers\.Authorization\s*=", source), relative_path


def test_refresh_retry_helper_refreshes_persists_and_retries_once_after_401() -> None:
    helper = read(AUTH_HELPER)
    auth_backend = read(AUTH_BACKEND)

    assert "EnsureAuthenticatedSessionAsync(cancellationToken)" in helper
    assert "response.StatusCode != HttpStatusCode.Unauthorized" in helper
    assert "RefreshAuthenticatedSessionOnceAsync(cancellationToken)" in helper
    assert "retrySessionResult.Status != AuthRefreshStatus.Success" in helper
    assert "AddBearerTokenIfPresent(retryRequest, retrySessionResult.Session.AccessToken)" in helper

    refresh_index = helper.index("RefreshAuthenticatedSessionOnceAsync(cancellationToken)")
    retry_send_index = helper.index("return await httpClient.SendAsync(retryRequest, cancellationToken)")
    assert refresh_index < retry_send_index

    assert "var refreshedSession = new StoredAuthSession" in auth_backend
    assert "RefreshToken = payload.RefreshToken" in auth_backend
    assert "await sessionStorageService.SaveAsync(refreshedSession, cancellationToken)" in auth_backend
    assert "return AuthRefreshResult.Success(refreshedSession)" in auth_backend


def test_subscription_status_regression_scenario_is_documented_in_code_path() -> None:
    """Covers the specific stale-token regression as an executable source invariant.

    Scenario: a stored session exists with an expired access token and valid refresh
    token; auth refresh returns rotated tokens; the central helper persists the new
    StoredAuthSession and sends the subscription retry with the refreshed access
    token rather than surfacing the first 401 as logout-worthy unauthorized state.
    """
    subscription_source = read(SUBSCRIPTION_CLIENT)
    helper = read(AUTH_HELPER)
    auth_backend = read(AUTH_BACKEND)
    subscription_order = [
        "HasStoredSessionAsync(cancellationToken)",
        "SendWithRefreshRetryAsync",
    ]
    helper_order = [
        "EnsureAuthenticatedSessionAsync(cancellationToken)",
        "RefreshAuthenticatedSessionOnceAsync(cancellationToken)",
        "AddBearerTokenIfPresent(retryRequest, retrySessionResult.Session.AccessToken)",
    ]
    backend_order = [
        "ShouldRefreshAccessToken(session)",
        "RefreshSessionAsync(cancellationToken)",
        "await sessionStorageService.SaveAsync(refreshedSession, cancellationToken)",
    ]

    for source, markers in [
        (subscription_source, subscription_order),
        (helper, helper_order),
        (auth_backend, backend_order),
    ]:
        position = -1
        for marker in markers:
            next_position = source.find(marker, position + 1)
            assert next_position != -1, f"Missing refresh-aware regression marker: {marker}"
            position = next_position
