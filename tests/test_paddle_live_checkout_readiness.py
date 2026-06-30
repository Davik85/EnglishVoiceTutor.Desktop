from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_pay_html_loads_paddle_js_and_has_no_server_secrets():
    html = read("site/public/pay.html")
    assert "https://cdn.paddle.com/paddle/v2/paddle.js" in html
    assert "Paddle.Initialize({ token: token })" in html
    assert "Paddle.Checkout.open({ transactionId: transactionId })" in html
    forbidden = ["api key", "ApiKey", "SecretKey", "webhook", "Bearer "]
    assert not any(value in html for value in forbidden)


def test_pay_html_reads_ptxn_and_blocks_missing_or_invalid_transaction():
    html = read("site/public/pay.html")
    assert 'get("_ptxn")' in html
    assert r"/^txn_[A-Za-z0-9_\-]+$/" in html
    assert "Checkout link is missing or invalid" in html


def test_pay_html_uses_public_config_placeholder_not_committed_token():
    html = read("site/public/pay.html")
    example = read("site/public/paddle.public.example.json")
    assert 'fetch("/paddle.public.json"' in html
    assert "REPLACE_WITH_PADDLE_LIVE_CLIENT_SIDE_TOKEN" in example
    assert "Checkout is not configured yet" in html


def test_backend_transaction_uses_public_pay_url_live_price_and_custom_data_markers():
    source = read("backend/EnglishVoiceTutor.Api/Services/Billing/PaddleBillingProviderCheckoutAdapter.cs")
    options = read("backend/EnglishVoiceTutor.Api/Options/PaddleBillingOptions.cs")
    assert "checkout = new" in source
    assert "url = configuredCheckoutUrl" in source
    assert "https://languagevoicetutor.com/pay.html" in options
    assert "PremiumLivePriceId" in options
    assert "GetPremiumPriceId(environment)" in source
    assert 'app = GetExpectedCustomDataApp()' in source
    assert 'product = GetExpectedCustomDataProduct()' in source


def test_webhook_reconciliation_requires_expected_price_product_and_custom_data():
    source = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventReconciliationDecisionService.cs")
    activation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventEntitlementActivationService.cs")
    for content in (source, activation):
        assert "MatchesExpectedPrice(metadata.PaddlePriceId)" in content
        assert "MatchesExpectedProduct(metadata.PaddleProductId)" in content
        assert "MatchesExpectedCustomData(metadata.CustomDataApp, metadata.CustomDataProduct)" in content
    normalizer = read("backend/EnglishVoiceTutor.Api/Services/Billing/PaddleWebhookEventNormalizer.cs")
    assert "customDataApp" in normalizer
    assert "customDataProduct" in normalizer
