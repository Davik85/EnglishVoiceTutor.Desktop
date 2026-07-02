from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_adjustment_events_are_normalized_and_processed_for_refunds_and_chargebacks():
    constants = read("backend/EnglishVoiceTutor.Api/Constants/SubscriptionConstants.cs")
    normalizer = read("backend/EnglishVoiceTutor.Api/Services/Billing/PaddleWebhookEventNormalizer.cs")
    reconciliation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventReconciliationDecisionService.cs")
    activation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventEntitlementActivationService.cs")

    assert 'AdjustmentCreated = "adjustment.created"' in constants
    assert 'AdjustmentUpdated = "adjustment.updated"' in constants
    assert "ExtractAdjustmentSnapshot" in normalizer
    assert "adjustmentAction" in normalizer
    assert "adjustmentStatus" in normalizer
    assert "adjustmentType" in normalizer
    assert "IsSupportedReconciliationEventType" in reconciliation
    assert "ProcessAdjustmentBillingEventAsync" in activation


def test_full_refund_and_chargeback_revoke_provider_entitlements_without_deleting_history_or_faking_webhooks():
    activation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventEntitlementActivationService.cs")

    assert 'metadata.AdjustmentAction, "refund"' in activation
    assert 'metadata.AdjustmentType, "full"' in activation
    assert 'metadata.AdjustmentAction, "chargeback"' in activation
    assert "SourceProviderEvent" in activation
    assert "StatusExpired" in activation
    assert "ExpiresAtUtc = nowUtc" in activation
    assert "PaddleFullRefundPremiumRevoke" in activation
    assert "PaddleChargebackPremiumRevoke" in activation
    assert "dbContext.Payments.Remove" not in activation
    assert "dbContext.Subscriptions.Remove" not in activation
    assert "PaddleWebhookEvents.Add" not in activation


def test_partial_refund_is_manual_review_and_safe_metadata_excludes_raw_provider_data():
    activation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventEntitlementActivationService.cs")
    normalizer = read("backend/EnglishVoiceTutor.Api/Services/Billing/PaddleWebhookEventNormalizer.cs")

    assert "PartialRefundManualReviewMessage" in activation
    assert "Premium unchanged" in read("backend/EnglishVoiceTutor.Api/Constants/SubscriptionConstants.cs")
    forbidden_in_admin_evidence = ["RawPayload", "SignatureHeader", "secret", "token", "cookie", "apiKey", "card"]
    evidence_block = activation.split("SafeMetadataJson = JsonSerializer.Serialize", 1)[1].split("}, MetadataJsonOptions)", 1)[0]
    assert not any(value in evidence_block for value in forbidden_in_admin_evidence)
    assert "rawPayload" not in normalizer.split("safeMetadata = new", 1)[1].split("};", 1)[0]


def test_existing_payment_success_failure_and_cancel_renewal_behaviors_remain_present():
    payment = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventPaymentPersistenceService.cs")
    activation = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventEntitlementActivationService.cs")
    subscription = read("backend/EnglishVoiceTutor.Api/Services/Billing/BillingEventSubscriptionSnapshotService.cs")

    assert "TransactionPaymentFailed" in payment
    assert "PaymentStatuses.Failed" in payment
    assert "TransactionCompleted" in activation
    assert "ActivatedReason" in activation
    assert "scheduledChangeAction" in read("backend/EnglishVoiceTutor.Api/Services/Billing/PaddleWebhookEventNormalizer.cs")
    assert "CancelAtPeriodEnd" in subscription
