#!/usr/bin/env python3
"""Local fake-signed Paddle refund/chargeback smoke helper.

This script intentionally does not create live Paddle payments or refunds. It signs
synthetic webhook JSON with a local/test Paddle webhook secret and posts it to a
local/test backend only. Use with a disposable local/test user and a non-production
backend/database.
"""
from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import sys
import time
import urllib.request
from typing import Any


def sign(body: str, secret: str, ts: int) -> str:
    mac = hmac.new(secret.encode("utf-8"), f"{ts}:{body}".encode("utf-8"), hashlib.sha256).hexdigest()
    return f"ts={ts};h1={mac}"


def post(base_url: str, secret: str, payload: dict[str, Any]) -> None:
    body = json.dumps(payload, separators=(",", ":"), sort_keys=True)
    ts = int(time.time())
    req = urllib.request.Request(
        base_url.rstrip("/") + "/api/billing/webhooks/paddle",
        data=body.encode("utf-8"),
        headers={"Content-Type": "application/json", "Paddle-Signature": sign(body, secret, ts)},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=30) as res:  # noqa: S310 local/test helper only
        print(res.status, res.read().decode("utf-8"))


def event(event_type: str, event_id: str, transaction_id: str, subscription_id: str, action: str, adjustment_type: str, amount: int) -> dict[str, Any]:
    return {
        "event_id": event_id,
        "event_type": event_type,
        "occurred_at": "2026-07-02T00:00:00Z",
        "data": {
            "id": event_id.replace("evt_", "adj_"),
            "action": action,
            "status": "approved",
            "type": adjustment_type,
            "transaction_id": transaction_id,
            "subscription_id": subscription_id,
            "items": [{"type": adjustment_type}],
            "totals": {"total": amount, "currency_code": "USD"},
        },
    }


def main() -> int:
    ap = argparse.ArgumentParser(description="Post fake-signed local/test Paddle adjustment webhooks; never use against production.")
    ap.add_argument("--base-url", default="http://localhost:5000")
    ap.add_argument("--secret", required=True, help="Local/test PaddleWebhook__SecretKey only")
    ap.add_argument("--transaction-id", required=True, help="Existing local/test ProviderPaymentId from a simulated transaction.completed")
    ap.add_argument("--subscription-id", required=True, help="Existing local/test ProviderSubscriptionId")
    args = ap.parse_args()

    if "languagevoicetutor.com" in args.base_url:
        raise SystemExit("Refusing to post fake-signed events to production/public host.")

    print("1. Ensure a local/test user already has Premium from a simulated transaction.completed.")
    post(args.base_url, args.secret, event("adjustment.updated", "evt_fake_full_refund", args.transaction_id, args.subscription_id, "refund", "full", -1499))
    print("2. Verify Premium is expired in Desktop/Admin Refresh status.")
    post(args.base_url, args.secret, event("adjustment.updated", "evt_fake_partial_refund", args.transaction_id, args.subscription_id, "refund", "partial", -100))
    print("3. Verify partial refund did not revoke or change already-expired state unexpectedly.")
    post(args.base_url, args.secret, event("adjustment.updated", "evt_fake_chargeback", args.transaction_id, args.subscription_id, "chargeback", "full", -1499))
    print("4. Verify chargeback expires active provider_event Premium when present.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
