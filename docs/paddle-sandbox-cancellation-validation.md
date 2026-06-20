# Controlled Paddle sandbox cancellation validation

This checklist is for controlled tester/sandbox validation only. It is not production/live Paddle readiness, not broad public release readiness, and not a refund, chargeback, or immediate Premium removal flow.

## Safety rules

- Backend remains the source of truth for account, subscription, entitlement, Premium/free status, payments, and cancellation state.
- Desktop does not call Paddle directly; it calls the backend Account billing endpoints only.
- Admin UI does not call Paddle directly; it calls the backend Admin support action only.
- Cancellation means cancel renewal / cancel at period end.
- Paid Premium remains active until `paidAccessUntilUtc` / the current paid period end.
- Failed provider cancellation must not revoke, expire, or delete Premium entitlements.
- Failed provider cancellation must not set `cancelAtPeriodEnd = Yes` or `renewalStatus = cancellation_scheduled` unless the provider has accepted/confirmed cancel-at-period-end.
- Do not put Paddle API keys, webhook secrets, provider tokens, connection strings, raw provider payloads, Authorization headers, customer secrets, or full provider subscription IDs in docs, UI, logs, tests, or generated files.

## Desktop cancellation path

1. Create or use a fresh tester account in the installed tester build configured for `https://api.languagevoicetutor.com`.
2. In Desktop Account settings, buy Premium through the backend-hosted Paddle checkout.
3. Wait for `transaction.completed` processing and the subscription snapshot to be reflected by the backend.
4. In Admin User Lookup, verify the before-cancellation diagnostics:
   - `premiumActive = Yes`
   - `billingProvider = paddle`
   - `renewalStatus = renewal_active`
   - `nextRenewalState = renewal_expected`
   - `hasActivePaidProviderSubscription = Yes`
   - `providerSubscriptionPresent = Yes`
   - `canRequestCancelRenewal = Yes`
   - `currentPeriodEndUtc` is set
   - `paidAccessUntilUtc` is set
5. In Desktop Account settings, choose Cancel subscription. Confirm the dialog that explains future renewals stop while paid Premium remains until the end of the current paid period.
6. Refresh Desktop Account status.
7. In Admin User Lookup, verify the after-cancellation diagnostics:
   - `renewalStatus = cancellation_scheduled`
   - `nextRenewalState = no_renewal_scheduled`
   - `cancelAtPeriodEnd = Yes`
   - `canRequestCancelRenewal = No`
   - `paidAccessUntilUtc` remains set
   - Premium remains active until the paid access end.

## Admin support cancellation path

Repeat the validation once with a separate fresh paid sandbox subscription:

1. Confirm the same before-cancellation diagnostics listed above.
2. Use the Admin **Cancel paid renewal** support action with a non-secret reason.
3. Verify Admin displays a success only when cancellation is confirmed.
4. Verify User Lookup shows the same after-cancellation diagnostics listed above.
5. Verify paid Premium remains active until `paidAccessUntilUtc`.

## Provider error investigation path

If the Admin support action returns `provider_error`:

- Treat the cancellation as not confirmed.
- Verify Admin shows the provider error as a warning/error, not a green success message.
- Verify `cancelAtPeriodEnd = No`, `renewalStatus` is not `cancellation_scheduled`, and `canRequestCancelRenewal = Yes` when retry is still meaningful.
- Capture only safe diagnostics for support triage:
  - `providerErrorCode`
  - `providerErrorMessageSafe`
  - `providerHttpStatusCode`
  - `providerRequestId` / correlation id when present and safe
  - `cancellationAttemptedAtUtc`
  - `providerSubscriptionPresent`
  - `providerSubscriptionIdLast4` or `providerSubscriptionIdHash`
- Do not capture or paste raw Paddle payloads, API keys, webhook secrets, Authorization headers, customer secrets, connection strings, or full provider subscription IDs.

## Paddle sandbox request-shape check

The backend provider adapter schedules renewal cancellation with Paddle Billing by sending a backend-only request to the subscription cancel endpoint:

- method: `POST`
- path shape: `/subscriptions/{providerSubscriptionId}/cancel`
- JSON body shape: `{ "effective_from": "next_billing_period" }`

This replaces the earlier update-subscription style `PATCH /subscriptions/{id}` request with a nested `scheduled_change` payload, which Paddle sandbox rejected with `bad_request` / `Invalid request` for the cancel-renewal operation. Validate this only in sandbox/tester diagnostics. Do not include real provider IDs, API keys, Authorization headers, raw provider payloads, or customer secrets in captured notes.
