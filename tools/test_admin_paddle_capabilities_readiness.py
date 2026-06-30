#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
service = (ROOT / "backend/EnglishVoiceTutor.Api/Services/Admin/AdminCapabilitiesService.cs").read_text()
snapshot = (ROOT / "backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminCapabilitiesSnapshot.cs").read_text()
admin_js = (ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js").read_text()
admin_html = (ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html").read_text()
tests = (ROOT / "backend/EnglishVoiceTutor.Api.Tests/Services/Admin/AdminCapabilitiesServiceTests.cs").read_text()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)

for expected in [
    "BillingProviderConfigured = billingProviderConfigured",
    "PaddleCheckoutAvailable = paddleCheckoutAvailable",
    "PaddleWebhooksAvailable = paddleWebhooksAvailable",
    "PaddleLiveConfigured = paddleLiveConfigured",
    "PaddleCheckoutUrlConfigured = paddleCheckoutUrlConfigured",
    "PaddleLivePriceConfigured = paddleLivePriceConfigured",
    "PaddleLiveProductConfigured = paddleLiveProductConfigured",
    "PaddleExpectedCustomDataConfigured = paddleExpectedCustomDataConfigured",
    "BillingLivePaymentTestComplete = false",
    "BillingPaidLaunchReleaseComplete = false",
]:
    require(expected in service, f"AdminCapabilitiesService missing {expected}")

for expected in [
    "public bool PaddleLiveConfigured",
    "public bool PaddleCheckoutUrlConfigured",
    "public bool PaddleLivePriceConfigured",
    "public bool PaddleLiveProductConfigured",
    "public bool PaddleExpectedCustomDataConfigured",
    "public bool BillingLivePaymentTestComplete",
    "public bool BillingPaidLaunchReleaseComplete",
]:
    require(expected in snapshot, f"AdminCapabilitiesSnapshot missing {expected}")

for forbidden in ["ApiKey =", "SecretKey =", "ClientSideToken ="]:
    require(forbidden not in service, f"Service must not return or assign secret field {forbidden}")

require("configured / live checkout opens / live payment test pending" in admin_js, "Admin UI must show configured live checkout with payment test pending")
require("unavailable / disabled" not in admin_html, "Admin UI must not hardcode Billing / Paddle as unavailable / disabled")
require("Assert.DoesNotContain(apiKey" in tests, "Tests must assert API key is not serialized")
require("Assert.DoesNotContain(webhookSecret" in tests, "Tests must assert webhook secret is not serialized")
require("BillingPaidLaunchReleaseComplete" in tests, "Tests must assert paid launch is not release-complete")
print("Admin Paddle capabilities readiness checks passed.")
