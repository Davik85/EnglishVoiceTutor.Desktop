# Subscription & Billing Foundation (Current State)

This document describes the current implemented foundation for account, trial, subscription, entitlement, free-limit enforcement, development test accounts, provider-agnostic checkout, Paddle billing/webhook ingestion, Paddle subscription/payment snapshots, entitlement activation/extension, canceled/paused expiry policy, and local Development CMS/admin support.

## Backend `.147` provider-precision production validation (2026-09-02)

Production backend `0.1.35-backend.147` is deployed and healthy. It successfully retained a genuine intermediate post-defer snapshot as `intermediate_expiry_not_yet_converged`, but the new plan later became `ambiguous_terminal`. Production database evidence showed the locally calculated target was internally consistent and that both the `DeferAsync` response and subsequent authoritative Google expiry agreed exactly with each other while differing from the local target by precisely 460 microseconds. Strict `DateTimeOffset` equality, rather than identity or linked-purchase evidence, caused this `.147` failure.

The source correction uses one Google-trial-deferral-only equivalence rule over UTC instants: an absolute difference strictly below one millisecond is target-equivalent, while one millisecond or more is not. It applies consistently to defer-response evidence, authoritative refresh, and unknown-outcome convergence. Baseline and command-ETag equality remain exact, arbitrary intermediate expiry never becomes authoritative, retries remain bounded and GET-only after provider-applied evidence, and completion persists the actual authoritative Google expiry rather than rewriting the stored target. Public rollout remains blocked until this correction is reviewed, deployed, and production-validated. No `.148` deployment, migration, production-row recovery, linked-purchase change, configuration change, or provider mutation is claimed.

## Backend `.146` initial-deferral production validation (2026-09-02)

Production backend `0.1.35-backend.146` is deployed and healthy, but validation did **not** close the initial Premium deferral blocker. A fresh explicitly allowed license-test purchase was verified and acknowledged, and its single defer mutation returned the intended target at provider precision. The immediate post-defer path nevertheless marked the plan `ambiguous_terminal` with `trial_deferral_provider_state_diverged` and no authoritative provider expiry; a normal subscriptions-v2 verification only seconds later persisted the same provider expiry. Related RTDN rows became `permanent_failure` / `provider_rejected` although the user had not canceled. This independently reproduced the same post-defer failure class previously seen with a real-money purchase.

The `.146` evidence proved intended-target acceptance at provider precision followed by later authoritative convergence to the same provider value, but it did not prove that expiry was the only field differing in the first post-defer snapshot. The `.147` source correction therefore kept only same-purchase, valid-lifecycle non-convergence retryable, preserved conclusive identity contradictions as terminal, performed GET-only bounded retries after provider-applied evidence, and completed only from a fresh authoritative target snapshot. At the `.146` checkpoint no `.147` deployment, row repair, migration, configuration change, Play Console change, or new provider mutation was claimed; the newer section above records `.147` deployment and its remaining strict-equality defect.

## Controlled Google Play RTDN and reconciliation foundation (2026-08-30)

Protected Google Play purchase-token persistence, authenticated RTDN receipt, reconciliation, linked-purchase-token replacement handling, the isolated pending-refund review pipeline, the trial-deferral foundation, and account-wide purchase-gating backend support are deployed in backend `0.1.35-backend.142`. Migrations `20260802154345_AddGooglePlayRtdnPersistenceFoundation`, `20260803052655_AddGooglePlayPendingRefundReviewFoundation`, and `20260827105749_AddGooglePlayTrialDeferralFoundation` are applied. The pending-refund module protects only token/order payloads, retains terminal protected material only for bounded cleanup, supports only explicit `NEUTRAL`, and does not fabricate usage evidence or directly alter Premium. Android Publisher authentication is provisioned through a service-account credential outside versioned release directories; OAuth and read-only provider access are verified.

For the approved Internal-testing license-test context, `GooglePlayBilling.Enabled`, `GooglePlayRtdn.Enabled`, and `GooglePlayReconciliation.Enabled` are enabled. The real Play-distributed versionCode 5 proved a controlled purchase -> backend verification -> backend-owned Premium path, and reconciliation later proved accelerated renewal extension of the same entitlement followed by final expiry and restored new-purchase eligibility. Mobile never grants Premium locally or acknowledges purchases. The earlier `configuration_invalid` catalog blocker is closed. Pending-refund review remains disabled unless independently verified; do not infer a reconciliation interval. This controlled evidence does not establish pending-payment, explicit cancellation, fresh-install restore, real-money, refund/voided-purchase, chargeback, broad rollout, or all provider-isolation cases.

Backend Data Protection is provisioned and enabled in production. Its persistent key ring is `/var/lib/languagevoicetutor/backend/data-protection/key-ring`, and its active certificate is `/etc/languagevoicetutor/data-protection/certificates/active/backend-data-protection.pfx`; both are outside versioned backend releases. The active PFX and its private key, protected backup integrity, and isolated restore drill were verified, and the backend restarted successfully with healthy public and database health checks. The key ring intentionally has no generated key XML until the first real protected-data use. One active certificate protects newly created keys, and source support for multiple previous certificates remains available to decrypt existing key-ring entries during a future rotation; no previous certificate is configured and no replacement is underway. This Data Protection provisioning does not enable Google Play Billing, RTDN, reconciliation, pending-refund review, or any purchase processing.

## Current tester release billing status

As of the latest controlled tester/sandbox validation snapshot, trial entitlement after registration is working, Paddle sandbox checkout works through backend-hosted checkout, and Desktop `v0.1.36-tester.24` has Account controls for Buy Premium, Cancel subscription, and Refresh status. This is not broad production/live billing readiness. Do not describe production checkout, production webhook operations, paid subscription lifecycle, public launch readiness, or billing support operations as broadly ready.

English Voice Tutor is a global, cross-platform, provider-agnostic product for desktop now and future mobile clients later. The backend is the source of truth for account, trial, subscription, entitlement, Premium/free status, daily free allowance, lesson history, usage, limits, payments, and billing state. Desktop and future mobile clients must rely on backend access/status decisions, not local payment assumptions.


## Current Step 4F status note

- Backend-hosted Paddle checkout launch page exists and is returned as a backend URL such as `/checkout/paddle?transactionId=...`.
- The manual sandbox payment loop has been validated: **Desktop Buy Premium -> backend checkout-session -> backend-hosted Paddle checkout in browser -> transaction.completed webhook -> backend entitlement active -> Desktop Refresh status -> lesson allowed**.
- A valid Paddle sandbox `transaction.completed` webhook activates Premium through the existing provider-event entitlement path.
- Transient PostgreSQL serialization conflicts in billing processing are retried, including subscription snapshot processing and entitlement activation paths.
- Premium access still comes from `EntitlementEntity`; `SubscriptionEntity` remains a provider-agnostic subscription snapshot only, and `PaymentEntity` remains diagnostic payment history only.
- This sandbox validation does not mea production billing setup is complete; production webhook setup verification, production checkout configuration, cancellation UX verification, and live Paddle readiness remain separate deferred work.


## Required base subscription plans

- The `free`, `trial`, and `premium` rows in the `plans` table are required reference data, not optional product-catalog content.
- Required values are `PlanId=free`, `DisplayName=Free`, `Tier=free`, `IsActive=true`; `PlanId=trial`, `DisplayName=Trial`, `Tier=premium`, `IsActive=true`; and `PlanId=premium`, `DisplayName=Premium`, `Tier=premium`, `IsActive=true`.
- Trial is a first-class tariff/plan for display and reference purposes. Trial is not a Paddle product and is not added to checkout price/product mapping.
- Missing base plan rows break subscription and entitlement writes through the `FK_subscriptions_plans_PlanId` and `FK_entitlements_plans_PlanId` constraints.
- EF migration `20260618090000_SeedBaseSubscriptionPlans` has idempotent PostgreSQL upsert SQL for the original base plans and EF migration `20260620090000_SeedTrialSubscriptionPlan` adds the required Trial reference row with idempotent PostgreSQL upsert SQL. The original base-plan migration is recorded in production `__EFMigrationsHistory`; the Trial migration is data/reference seed only and makes fresh databases and previously repaired databases converge without duplicate plan rows.
- The seed keeps both base plans active and does not delete other plan rows. Database update remains an explicit operator step; packaging and backend upload scripts must not apply migrations automatically.
- Free, trial, and Premium status logic remains backend-owned. Premium access is determined by entitlement rows, not by Desktop local state, Paddle checkout state, or Paddle subscription snapshots directly.

## Learner Account subscription UI

- The learner Account subscription block is intentionally simplified to four customer-facing lines: current tariff, free lessons remaining, Premium status, and auto-renewal.
- Trial is displayed as tariff `Trial`, including when paid or manual Premium is scheduled for after trial expiry.
- The learner Premium line is a backend-computed display summary of the current continuous Premium coverage window. It can show coverage through active Trial/Premium access plus adjacent queued `provider_event` or `manual_admin` Premium entitlements; gaps stop the display chain.
- This continuous coverage date is display-only and does not change access authority: `PremiumActive` still comes only from currently started, unexpired Premium `EntitlementEntity` rows, and future-start entitlements do not grant access before `StartsAtUtc`.
- Admin diagnostics remain the place to inspect detailed entitlement schedules, sources, provider events, renewal/cancellation details, and raw timing.
- Trial and Premium show free lessons as unlimited/without limits because active entitlement access bypasses the daily free lesson counter.
- Renewal internals, cancellation explanations, provider-subscription presence, source/authenticated/enforcement/checked-at values, scheduled paid Premium starts/ends, and Paddle diagnostics remain Admin/diagnostic concerns and are not rendered in the learner Account block.
- Backend `.138` changed no Desktop or Mobile production code and introduced no new trial-status line. Existing clients continue to render the backend-owned final Premium expiry in their existing layouts.

## Account requirement for normal lesson start

- Normal lesson start requires sign-in.
- Desktop blocks signed-out users from starting a normal lesson and asks them to sign in or create an account.
- Development signed-out fallback paths still exist for diagnostics/development flows, but they are not the normal desktop UX path.

## Trial behavior

- A **7-day Premium trial** starts automatically after successful registration.
- Trial access remains entitlement-owned: active trial grants/entitlements decide access, while the `trial` plan row gives learner-facing UI and reference data a real tariff named `Trial`. Because its tier is `premium`, active Trial behaves as Premium for lesson limits during the trial period.
- Trial is **account-level** and intended to be shared by this account across desktop and future mobile clients.
- Login does **not** create or extend trial.
- `POST /api/me/trial/claim` still exists as a fallback/manual endpoint and remains one-trial-per-account.

## Free plan behavior

- Free plan currently allows **1 free lesson per day**.
- A free lesson is consumed only after both conditions are true in the same lesson session:
  1. Lesson session has started.
  2. Learner sends at least 3 valid user messages.
- Current lesson continuation is not blocked by this daily free-limit rule.
- Starting another new lesson can be blocked when subscription enforcement is enabled.

## Premium behavior

- Active Premium entitlement allows lesson start.
- Premium access bypasses free-lesson daily limits.
- `EntitlementEntity` remains the source of Premium access.
- Premium can come from trial, Development test-account grants, local admin grants, or validated provider billing events.
- Valid provider billing activation creates or extends `provider_event` Premium `EntitlementEntity` rows from validated `reconciliation_pending` `transaction.completed` `billing_events` only.
- Provider-event paid Premium stacks after active trial/Premium access. If a user buys Premium during an active trial, paid Premium starts after `trialEndsAtUtc` and preserves the paid duration; a future-start provider-event entitlement does not count as active Premium until its `StartsAtUtc` is reached, so the trial remains the current access source until trial expiry.
- Later valid period ends extend existing provider-event access; duplicate or older events do not duplicate or shorten entitlement.
- Actual `subscription.canceled` and `subscription.paused` events expire only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Scheduled cancellation and past-due snapshots do not revoke Premium early.
- Manual/admin/trial/development/future-mobile entitlements are not touched by the provider-event canceled/paused expiry path.
- In local Development, a configured development test account can simulate unlimited Premium entitlement.

## Restored trial/manual Premium extension behavior

Backend `0.1.35-backend.138` restored the established Premium extension behavior; it did not add a new subscription scheduling system.

- `AdminPremiumGrantService` must consider both active account trials in `TrialGrants` and applicable active Premium `Entitlements`.
- For an active trial ending at `T1`, a new manual grant starts at `T1` and its expiry is calculated by adding the requested duration to `T1`, not to the current time.
- If an applicable Premium entitlement ends later than the trial, that later expiry remains the extension base.
- Expired, revoked, inactive, and other-user records do not extend the grant.
- Subscription status must preserve the later final Premium coverage expiry while the trial remains current access.
- A status read is read-only: it must not repair or rewrite entitlement dates. An existing overlapping record can still report its later stored expiry without database mutation.
- A scheduled future entitlement is visible in **Premium Entitlement Schedule** but is correctly absent from **Active Entitlements** until `StartsAtUtc`.
- Manual Premium remains provider-neutral and must not expose Paddle or Google Play metadata, auto-renewal, or cancellation controls.

## Enforcement model

- Config key: `SubscriptionEnforcement:Enabled`
- Committed default value remains: `false`
- Local override example:
  - `SubscriptionEnforcement__Enabled=true`
- Backend is the enforcement authority for lesson-start access.
- Desktop preflight checks improve UX, but the backend remains the source of truth.
- Existing/current lesson continuation is not blocked by this enforcement; enforcement is about starting new lessons.

## Development unlimited Premium test accounts

- Config section: `DevelopmentTestAccounts`
- Local env var example (placeholder only):
  - `DevelopmentTestAccounts__UnlimitedPremiumEmails__0="test@example.com"`
- Development-only behavior.
- When matched, backend creates or updates a Premium entitlement using source `development_test_account`.
- Do not commit personal test emails.

## Billing checkout skeleton (provider-agnostic foundation)

Endpoint:
- `POST /api/me/billing/checkout-session`

Current disabled/default behavior:
- Requires authorization; unauthenticated requests return `401`.
- Expected invalid checkout requests return `400` with safe validation payloads and are handled at the endpoint layer, not as unhandled server exceptions.
- When no provider is configured, the endpoint returns a disabled/provider-not-configured response:
  - `created=false`
  - `checkoutEnabled=false`
  - `provider=none`
  - `errorCode=billing_provider_not_configured`
  - `message="Billing checkout is not configured yet."`
- No external payment provider call is made in the disabled/default path.
- No billing event is written in the disabled/default path.
- No `SubscriptionEntity`, `PaymentEntity`, or `EntitlementEntity` state is mutated in the disabled/default path.

Current config section:

```json
"Billing": {
  "CheckoutEnabled": false,
  "Provider": "none",
  "SuccessUrl": "",
  "CancelUrl": ""
}
```

## Provider-agnostic billing direction

- Internal plan ids stay independent from provider-specific plan/price ids.
- Paddle is the current web/desktop checkout provider adapter, but the integration stays adapter-based.
- Paddle must not be hardcoded into lesson/business access logic.
- Future entitlement/billing sources may include:
  - Paddle for web/desktop checkout.
  - Apple App Store for future iOS.
  - Google Play for future Android.
  - Manual admin grants via CMS/admin tooling.
- `GooglePlayPurchaseVerificationService` is connected to `IGooglePlayVerifiedPurchasePersistenceService`. The atomic orchestration establishes token-fingerprint ownership, selects or creates the exact `google_play` Subscription, and persists only that Subscription and its linked `provider_event` Premium entitlement in one serializable transaction.
- Sanitized subscriptions-v2 lifecycle projection has these exact access semantics: `ACTIVE` and `IN_GRACE_PERIOD` retain/restore/extend the exact Google entitlement; future-expiry `CANCELED` retains it through the verified expiry and records non-renewal metadata; effective `ON_HOLD` and `PAUSED` make only that entitlement inactive; `EXPIRED` and past-expiry `CANCELED` expire only that entitlement. A later trusted `ACTIVE` state restores a hold/paused entitlement, while a revoked token cannot restore itself.
- RTDN triggers a fresh subscriptions-v2 verification, whose result is the current lifecycle source of truth. Authenticated `SUBSCRIPTION_REVOKED` supplies confirmed-revocation context: fresh `EXPIRED` may be persisted as revoked, while entitlement-retaining, ambiguous, invalid, unknown, malformed, or unsupported fresh results do not revoke existing access. Temporary provider failure preserves access and remains retryable. A full-refund subscription `VoidedPurchaseNotification` is stored as a sanitized refund signal and triggers normal reconciliation, but refund alone is not proof of subscription entitlement revocation.
- This lifecycle projection adds no plan, entitlement type, Payment/BillingEvent projection, or schema. Deployed account-status multi-provider selection remains entitlement-driven, so Google lifecycle changes cannot shorten, hide, relabel, or revoke valid Paddle, `manual_admin`, or trial Premium.
- The disabled Google Play infrastructure, account-wide Premium coverage, and purchase gate are deployed through production backend `.142`; migrations `20260727045935_AddGooglePlayPurchaseClaims`, `20260802154345_AddGooglePlayRtdnPersistenceFoundation`, `20260803052655_AddGooglePlayPendingRefundReviewFoundation`, and `20260827105749_AddGooglePlayTrialDeferralFoundation` are applied. Runtime purchase processing remains dormant because Google Play is disabled; no enablement, production configuration, Google credentials, or provider mutation was made by this rollout.
- The applied additive migration `20260827105749_AddGooglePlayTrialDeferralFoundation` created the isolated `google_play_initial_premium_deferrals` table with a unique `GooglePlayPurchaseClaimId`, so one claim cannot receive its initial account-coverage deferral twice. The row stores the provider purchase start/baseline, original continuous Premium coverage start/tail, immutable approved duration/target, license-test marker, exact mutation ETag, bounded retry metadata, supporting defer-response expiry, authoritative refreshed expiry, and terminal state.
- Production deferral is eligible only for exact Product ID `premium` and exact Base Plan ID `monthly` when subscriptions-v2 proves one active, paid, base-price, auto-renew-enabled line item in its initial 28-to-31-day monthly period, with successful order evidence, no Play test purchase, no linked token, no prepaid plan, no offer ID, no Google free-trial/introductory phase, no signup promotion, and no item replacement/removal/deferred replacement. Missing or conflicting evidence fails closed: the Google purchase may still follow its normal exact-provider lifecycle, but no defer mutation is sent.
- The provider-neutral calculator finds the account's continuous Premium tail at provider `StartTime` from the active backend trial plus contiguous active/scheduled `manual_admin` and `provider_event` Premium. Required defer is `existing coverage tail - provider StartTime`, so the complete first paid Google duration lands after already-owned coverage. Non-positive duration creates no plan; positive duration below 24 hours becomes exactly 24 hours; duration of at least 24 hours uses the actual duration, up to Google's one-year per-call maximum.
- Account-wide Premium coverage calculation is provider-neutral, but provider mutation is provider-specific. Paddle may use all valid Trial, `manual_admin`, Paddle, and Google coverage to calculate the continuous tail, while Paddle activation, lifecycle expiry, full-refund, and chargeback may mutate only an entitlement linked to the exact Paddle `Subscription`. Google lifecycle remains exact-Google-owned. Legacy unscoped `provider_event` ownership is never guessed: null-`SubscriptionId` rows are not retroactively claimed, and ambiguous Paddle adjustment ownership fails closed for manual review.
- New Google Play purchase eligibility is a separate provider-renewal decision, not a Premium-access decision. The additive authenticated `SubscriptionStatus` gate evaluates all Premium provider subscriptions for the account, not only the effective provider displayed to a client. Trial, `manual_admin`, scheduled fixed coverage, terminal non-renewing provider state, and a proven current Paddle cancel-at-period-end state do not own future renewal. For active/trialing Paddle, proof requires the subscription's latest provider event to match the processed Paddle `BillingEvent` and its safe metadata for event, user, and subscription identity, with `scheduledChangeSnapshotComplete == true`, action `cancel`, and an effective time strictly after the status check. Legacy sticky cancellation fields or missing/mismatched evidence fail closed; an already-reached effective time also fails closed until a current terminal/provider snapshot arrives. Paddle normalizes the required current `scheduled_change` member separately from missing/incomplete evidence: a complete `subscription.updated` snapshot with explicit `scheduled_change: null` replaces and clears an earlier cancel/pause/resume, whereas a missing, malformed, unsupported, or incomplete member blocks snapshot persistence without guessing removal. Active renewal after authoritative removal blocks. Payment-retry/past-due and paused/resumable states always remain blocking and are evaluated before cancellation metadata. Other recoverable external provider relationships also block; multiple apparent renewal owners, missing provider identity, unknown state, and cancellation/status conflicts fail closed. The safe blocking provider is returned only for one unambiguous owner. This deployed gate neither mutates provider state nor changes `PremiumCoverageTimeline`; restore and exact Google purchase-token reverification bypass it. The [mandatory billing-adapter regression gate](#mandatory-billing-adapter-regression-gate) remains required before acceptance or enablement.
- Current Google documentation gives license-test monthly subscriptions accelerated renewal periods and explicitly directs license-test scenarios to defer billing when needed. An accelerated-period deferral-evidence path therefore exists only when the subscriptions-v2 purchase is marked as a test purchase and the authenticated user passes both existing `TestPurchasesEnabled` and `AllowedTestPurchaseUserIds` controls. It still requires exact package/product/base-plan/base-price/auto-renewing shape and is never available to a production purchase.
- Acknowledgement is independent and remains first priority. The immutable plan is captured with normal purchase persistence, but `purchases.subscriptionsv2.defer` is not sent until acknowledgement is confirmed and a fresh subscriptions-v2 GET supplies the current baseline expiry and ETag. Only the v2 defer API is used; the deprecated `purchases.subscriptions.defer` is not used.
- Exactly-once recovery stores the one command before provider mutation. Unknown outcomes and ETag conflicts re-read provider state: target-equivalent expiry converges to provider-applied handling, while exact baseline plus the unchanged stored command ETag remains the only path that permits a bounded retry of that same command. Target equivalence is narrowly fixed to Google trial deferral and compares UTC instants with an absolute difference strictly below one millisecond; one millisecond or more is not equivalent. After a successful target-equivalent defer response, a same-purchase, valid-lifecycle baseline or intermediate expiry remains bounded and GET-only retryable; conclusive start/product/linked-purchase identity contradictions remain ambiguous-terminal with no further mutation. Temporarily unavailable or structurally unusable GETs also remain bounded and GET-only. Safe assessment logs expose classification only: `target_converged`, `baseline_not_yet_converged`, `intermediate_expiry_not_yet_converged`, `temporarily_unusable`, or `identity_contradiction`. The defer response remains supporting evidence only: the existing exact Google persistence path receives the actual Google expiry only after a fresh authoritative GET confirms a target-equivalent value. If that confirming snapshot is already future-expiry `CANCELED`, persistence retains the cancellation lifecycle and access through the actual provider expiry instead of restoring `ACTIVE`. No local baseline-plus-duration entitlement is synthesized, and non-convergence fails closed at the existing retry limit.
- Controlled Google Play Billing, RTDN, and reconciliation are enabled for the approved Internal-testing license-test context; pending-refund review remains disabled. The trial-deferral migration and backend `.142` deployment are complete, and production has since advanced to `.147`. Backend Data Protection and Android Publisher authentication are provisioned; controlled purchase, provider mutation, accelerated renewals, final expiry, and a real-money first purchase are verified. Initial-deferral provider-precision convergence remains a public-rollout blocker until the corrected source behavior is deployed and controlled validation passes.
- The backend remains the source of truth for entitlement state.

Google Play is an additional billing provider, not a replacement for the provider-neutral entitlement calculation. Google Play adapter work must not shorten, hide, or relabel valid trial, `manual_admin`, or Paddle Premium. Selecting provider-specific `Subscription` metadata and calculating common Premium access/coverage are separate responsibilities. Every Google Play change must pass the shared mandatory billing-adapter regression gate below before acceptance.

## Mandatory billing-adapter regression gate

This gate is mandatory for Paddle changes, Google Play changes, future Apple App Store or other provider adapters, provider-selection logic, purchase persistence, entitlement creation, subscription-status calculation, and cancellation, renewal, refund, restore, replacement, or replay handling. Provider-specific tests alone are insufficient.

Invariant: **Premium periods extend the single account-wide continuous coverage timeline; paid/granted duration must not be consumed by accidental overlap.**

### A. Premium grant timing

Verify separately:

- active trial followed by manual Premium;
- manual Premium followed by another manual Premium;
- provider Premium followed by manual Premium;
- manual Premium followed by provider Premium where supported;
- expired, revoked, inactive, and other-user records do not extend access;
- each new period starts from the correct existing coverage end rather than from the current time.

### B. Subscription and entitlement stacking

Verify both **subscription/Premium period stacking** and **Premium coverage aggregation**:

- adjacent Premium periods extend rather than overwrite one another;
- overlapping periods preserve the later final expiry;
- a shorter new provider record cannot shorten valid existing Premium;
- Paddle, Google Play, `manual_admin`, and trial remain parts of one backend-owned Premium source of truth;
- provider metadata selection does not replace provider-neutral access calculation;
- an orphan provider `Subscription` without a valid linked entitlement does not grant Premium.

### C. User-visible subscription status

For every supported client affected by the contract, verify:

- the final Premium expiry shown to the user;
- trial expiry is not substituted for the later final Premium expiry;
- current tariff/source remains correct;
- Auto-renew and cancellation controls match the real provider state;
- manual Premium is not presented as Paddle or Google Play;
- no unapproved UI element or status line is added;
- Desktop and Mobile retain their existing layouts unless a separate UI task is approved.

### D. Admin and support visibility

Verify **Premium Entitlement Schedule**, **Active Entitlements**, current-versus-future entitlement distinction, provider/renewal state, and final expiry. A future scheduled entitlement is expected to be absent from **Active Entitlements** before `StartsAtUtc`; that absence is not a defect.

### E. Required automated regression coverage

Billing-adapter work is not complete with provider-specific tests alone. Select and run the existing focused coverage, using repository test filters/conventions, for:

- `AdminPremiumGrantService`;
- `SubscriptionStatusService`;
- the affected provider adapter, verification, and persistence path;
- Paddle behavior when Google Play or shared subscription logic changes;
- Google Play behavior when Paddle or shared subscription logic changes;
- client JSON parsing and display when the response contract or status calculation changes.

Typical backend filters include `FullyQualifiedName~AdminPremiumGrant`, `FullyQualifiedName~SubscriptionStatusServiceTests`, `FullyQualifiedName~GooglePlay`, and `FullyQualifiedName~Paddle`. Use the focused Desktop policy checks and Mobile model/widget tests that own the affected display contract. Do not hardcode pass counts as policy.

### F. Required controlled manual smoke

Any billing-adapter change that can affect subscription status requires at least one controlled account with:

- a current trial;
- scheduled or active manual Premium;
- final Premium expiry later than the trial;
- correct provider and Auto-renew state;
- correct affected Desktop or Mobile display;
- correct Admin CMS entitlement schedule and current/future distinction.

Provider sandbox checks alone are insufficient. Record only non-identifying evidence.

## Paddle checkout transaction creation v1

Endpoint:
- `POST /api/me/billing/checkout-session`

Current completed behavior:
- Backend can create a Paddle sandbox/live checkout transaction only when explicitly configured.
- Paddle checkout is controlled by all of these settings being supplied outside client code:
  - `Billing__CheckoutEnabled=true`
  - `Billing__Provider=paddle`
  - `PaddleBilling__CheckoutAdapterEnabled=true`
  - `PaddleBilling__Environment=sandbox` or `PaddleBilling__Environment=live`
  - `PaddleBilling__ApiKey=<secret>`
  - `PaddleBilling__PremiumPriceId=<price id>`
  - `PaddleBilling__ClientSideToken=<public client-side token>`
- The backend calls the Paddle sandbox/live API to create a transaction and returns a backend-hosted launch page URL such as `/checkout/paddle?transactionId=txn_...` as `checkoutUrl`.
- Paddle API keys and price ids must be stored in environment variables, user secrets, or secure deployment configuration; never store real values in tracked files or client code. The client-side token is public but should still be supplied by backend configuration and never hardcoded into desktop.
- The backend-hosted launch page loads Paddle.js from `https://cdn.paddle.com/paddle/v2/paddle.js`, initializes Paddle in sandbox/live mode with `PaddleBilling__ClientSideToken`, and opens checkout for the transaction id.
- Checkout transaction creation does **not** itself activate Premium.
- Checkout transaction creation does **not** create internal `billing_events`.
- Checkout transaction creation does **not** mutate `SubscriptionEntity`.
- Checkout transaction creation does **not** mutate `PaymentEntity`.
- Checkout transaction creation does **not** mutate `EntitlementEntity`.
- Entitlement activation is separate and can activate Premium only after webhook ingestion, normalization, reconciliation decision processing, and strict activation validation.
- Optional real sandbox smoke script: `tools/smoke_paddle_checkout_live_sandbox.ps1`.
- The optional real sandbox smoke requires `-AllowRealPaddleCall` and creates a real Paddle sandbox transaction only; it does not complete payment, call webhooks, or activate internal entitlement state.


## Current-user cancel-renewal foundation

Endpoint:
- Authenticated current-user billing cancellation endpoint for cancel-renewal/cancel-at-period-end.

Current completed behavior:
- Cancellation is backend-owned; Desktop does not send an arbitrary Paddle/provider subscription id.
- The backend uses the current user's subscription snapshot to resolve the provider subscription context.
- The Paddle adapter supports cancel-at-period-end/next-billing-period behavior for sandbox validation.
- A cancel request must mean cancel renewal, not immediate removal of paid access.
- The cancel request path must not directly revoke `EntitlementEntity`.
- Existing paid Premium or scheduled paid Premium remains until entitlement expiry unless a later explicitly designed provider lifecycle/refund/reversal path changes it.
- Current-user subscription status exposes the cancellation and future-Premium fields needed by Desktop Account UI decisions, including scheduled cancellation/action/effective dates, current period end, entitlement expiry, future-start Premium, and enough state to show Buy/Cancel/Refresh decisions.

## Paddle webhook ingestion foundation v1

Endpoint:
- `POST /api/billing/webhooks/paddle`

Current completed behavior:
- The endpoint exists for Paddle webhook ingestion.
- The endpoint is protected by Paddle `Paddle-Signature` verification, not JWT authentication.
- The endpoint uses the raw request body before JSON parsing because Paddle signs `<timestamp>:<raw_body>`.
- Signature verification uses `ts` and `h1` values from `Paddle-Signature`, HMAC-SHA256 with the configured notification destination secret, timing-safe comparison, and timestamp tolerance.
- If `PaddleWebhook:Enabled=false`, the endpoint returns `404` to hide the disabled webhook endpoint.
- If the endpoint is enabled but the secret is blank, it returns `503` with the safe message `Paddle webhook is not configured.`
- Missing, invalid, or stale signatures return `401`.
- Invalid JSON after valid signature verification returns `400`.
- Raw Paddle webhook events are stored in `paddle_webhook_events`.
- Duplicate Paddle event ids are accepted idempotently and return `200` with `duplicate=true` without inserting a second raw event row.
- New valid events return `200` with `accepted=true` and `duplicate=false`.
- After durable raw ingestion, the request flow runs normalization, subscription/payment snapshot processing, reconciliation decision processing, and entitlement activation/expiry policy for the current provider event only.
- Premium entitlement activation is allowed only from validated provider-agnostic `billing_events`; raw webhook payloads are not used directly for access-state mutation.
- The Paddle webhook secret must be stored in environment variables, user secrets, or secure deployment configuration. Do not put real webhook secrets in tracked files or client code.

Safe local configuration example:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

## Billing event normalization foundation v1

- Accepted Paddle webhooks are normalized into provider-agnostic `billing_events` rows after the raw webhook event is safely stored in `paddle_webhook_events`.
- `paddle_webhook_events` remains the raw signed event ingestion table and stores the original webhook payload separately from the normalized billing stream.
- `billing_events` is the provider-agnostic event stream for reconciliation decisions and stores safe metadata only, not raw payloads, signatures, API keys, webhook secrets, or other secrets.
- Normalization is idempotent through the existing unique `billing_events` constraint on provider + provider event id.
- Normalization does not call Paddle.
- Normalization carries safe subscription/payment/entitlement metadata used by downstream provider-agnostic processing.
- Normalization does not directly grant Premium access.

## Subscription lifecycle snapshot foundation v1

- Paddle `subscription.created` and `subscription.updated` events upsert a provider-agnostic `SubscriptionEntity` snapshot for an existing internal user.
- Duplicate `subscription.created` is idempotent.
- `subscription.updated` updates provider snapshot/current period data.
- Older out-of-order `subscription.updated` events do not regress `SubscriptionEntity` state.
- `subscription.created` and `subscription.updated` do not activate Premium by themselves.
- Scheduled cancellation metadata from `subscription.updated` is recorded in `SubscriptionEntity`.
- `cancelAtPeriodEnd`, `scheduledChangeAction`, and `scheduledChangeEffectiveAtUtc` are exposed safely where needed for diagnostics/status.
- Scheduled cancellation does not revoke Premium early.
- Scheduled cancellation does not shorten existing `provider_event` entitlement.
- Paddle `subscription.past_due` is recorded as `SubscriptionEntity` snapshot/status.
- `subscription.past_due` does not create entitlement.
- `subscription.past_due` does not extend entitlement.
- `subscription.past_due` does not revoke already active entitlement.
- A user without active entitlement does not become Premium from `subscription.past_due`.
- Actual `subscription.canceled` updates `SubscriptionEntity.Status = Canceled`.
- Actual `subscription.paused` updates `SubscriptionEntity.Status = Paused`.
- `SubscriptionEntity` stores subscription lifecycle data only and does not grant Premium access by itself.
- `subscription.resumed` and `subscription.activated` update only `SubscriptionEntity` snapshot/status to active and do not restore Premium by themselves.
- Premium restoration still requires valid `transaction.completed` through the existing entitlement activation/extension path.

## Payment persistence snapshot foundation v1

- Paddle `transaction.completed` and `transaction.payment_failed` events upsert a minimal provider-agnostic `PaymentEntity` diagnostic snapshot for an existing internal user when safe metadata maps to the Premium plan.
- `PaymentEntity` stores provider-agnostic payment/transaction trail.
- Payment snapshots are idempotent by billing provider + provider transaction id.
- Duplicate `transaction.completed` and `transaction.payment_failed` events do not duplicate `PaymentEntity`.
- `transaction.completed` stores a `completed` payment snapshot and still relies on the existing entitlement activation flow for Premium access.
- `transaction.payment_failed` stores a `failed` payment snapshot and does not activate Premium.
- `PaymentEntity` is diagnostic payment history only and is not used as an access source.

## Entitlement reconciliation decision foundation v1

- Normalized `billing_events` are inspected for entitlement reconciliation eligibility.
- Paddle `transaction.completed` events with an existing internal user and safe metadata containing `internalPlanId=premium` become `reconciliation_pending`.
- Unsupported billing event types are marked `ignored`.
- Malformed, invalid, or missing required safe metadata is blocked and does not become eligible for activation.
- This is provider-agnostic processing over `billing_events`.
- This step does not activate Premium by itself.
- This step does not use `SubscriptionEntity` or `PaymentEntity` as an access source.
- This step does not call Paddle.
- Raw webhook events remain in `paddle_webhook_events`.

## Entitlement activation and extension foundation v1

- Valid `reconciliation_pending` Paddle `transaction.completed` events can create Premium `EntitlementEntity` rows.
- Valid later `transaction.completed` events can extend an existing `provider_event` Premium entitlement.
- Duplicate `transaction.completed` does not duplicate entitlement.
- Older `transaction.completed` does not shorten entitlement.
- Activation currently requires:
  - `provider=paddle`;
  - `eventType=transaction.completed`;
  - a valid `internalUserId` in safe metadata;
  - an existing internal user for that id;
  - `internalPlanId=premium`;
  - `billingPeriodEndsAtUtc` present, parseable, and in the future;
  - `source=provider_event`.
- `billingPeriodStartsAtUtc` is used as the entitlement start when present and valid; otherwise activation uses the current UTC processing time.
- Activation creates or extends a Premium `EntitlementEntity` with source `provider_event`, status `active`, and a safe reason referencing the provider event.
- Existing events without a future `billingPeriodEndsAtUtc` are blocked instead of granting open-ended Premium.
- Activation does not read `PaymentEntity` and does not use it as an access source.
- Activation does not call Paddle.
- Raw Paddle webhook events remain in `paddle_webhook_events`.
- Provider-agnostic `billing_events` remain the processing source for reconciliation decisions and entitlement activation.

## Actual canceled / paused expiry policy foundation v1

- Actual `subscription.canceled` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Actual `subscription.paused` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Manual/admin/trial/development/future-mobile entitlements are not touched by this provider-event expiry path.
- `subscription.resumed` and `subscription.activated` are snapshot-only active-status events and do not reverse provider-event entitlement expiry by themselves.

## Event-scoped webhook request processing

- A Paddle webhook request processes only the current Paddle provider event id.
- The request normalizes only the current Paddle event.
- The request processes subscription lifecycle snapshot logic only for the current provider event.
- The request processes payment snapshot logic only for the current provider event.
- The request processes reconciliation decision logic only for the current provider event.
- The request activates, extends, or expires entitlement only for the current provider event when strict validation passes.
- The request must not process old unrelated `received` or `reconciliation_pending` billing events.
- Broad/batch processing is reserved for future worker/backfill tooling.

## Backend access/status verification

- Backend access/status endpoints recognize provider-event Premium entitlement as backend state.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` alone must not grant Premium access.
- `PaymentEntity` must not grant Premium access.
- Desktop and future mobile clients must rely on backend state, not local payment assumptions.
- Desktop upgrade/paywall UI exists for sandbox validation and remains backend-driven; desktop still does not activate Premium locally.
- Future mobile UI was not added for this foundation step.

## CMS/admin support foundation v1 checkpoint

- CMS/admin support foundation v1 is completed for local Development support workflows.
- It is not a production RBAC/admin system.
- It uses Development/config bootstrap admin access.
- The local admin shell is backend-hosted at `/admin/`.
- Tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, CMS Content, and System.
- Admin shell JWT remains memory-only. Refresh auth continues to use the existing admin-only HTTP-only cookie, and selected workspace state is restored only from non-secret URL hash fields after the admin session is valid.
- URL hash workspace restore covers the active admin tab, CMS sub-tab, content pack slug, CMS selected entity keys, and selected admin user ID. Unsaved form content, prompt bodies, full scenario JSON, tutor profile JSON, passwords, and tokens are not browser-persisted.
- CMS draft editors show an unsaved-change indicator and warn before refresh/close, tab switches, CMS sub-tab switches, selecting another entity, publish/restore reload flows, or logout. Save draft remains the explicit persistence action; unsaved content is not stored in browser storage or the URL hash. CMS draft-save audit logging is implemented for successful Save draft operations, with smoke/test audit entries hidden by default and a debugging checkbox to show them. Structured scenario editing, required publish summary validation, publish discoverability, and the local runtime published-snapshot read path are complete for development/admin product scope. The next recommended CMS implementation step is scenario editor usability refinement, not billing, while production RBAC and approval workflow remain future work. The admin shell is still development/admin-only and not production RBAC.
- It supports:
  - Exact user lookup by email.
  - Read-only user diagnostics.
  - Premium entitlement schedule inspection.
  - Manual Premium grant.
  - Manual Premium revoke.
  - Free lesson allowance reset.
  - Read-only audit log.
  - Capabilities view.
- Admin mutations require a reason and write audit actions.
- Static admin shell audit script (`tools/audit_admin_shell.ps1`) guards required tabs/forms/controls/endpoints and forbids `localStorage`/`sessionStorage` usage in `admin.js`.
- Smoke script (`tools/smoke_admin_foundation.ps1`) runs the admin shell audit first.
- Admin UI was not changed for Paddle lifecycle documentation.

## Controlled Paddle sandbox cancellation validation

A focused manual checklist for Desktop and Admin cancel-renewal validation is maintained in `docs/paddle-sandbox-cancellation-validation.md`. That checklist is controlled tester/sandbox only and does not imply production/live Paddle readiness. It explicitly requires paid Premium to remain active until the paid access end, and it describes the safe `provider_error` diagnostics path without secrets, raw provider payloads, Authorization headers, customer secrets, connection strings, or full provider subscription IDs.

## Current smoke scripts

- `tools/smoke_billing_checkout.ps1`
- `tools/smoke_paddle_checkout_adapter.ps1`
- `tools/smoke_paddle_checkout_live_sandbox.ps1`
- `tools/smoke_paddle_webhook_ingestion.ps1`
- `tools/smoke_admin_foundation.ps1`
- `tools/smoke_paddle_subscription_lifecycle.ps1`
- `tools/smoke_paddle_payment_persistence.ps1`
- `tools/smoke_paddle_entitlement_extension.ps1`
- `tools/smoke_paddle_cancellation_past_due_policy.ps1`
- `tools/smoke_paddle_canceled_paused_expiry_policy.ps1`
- `tools/smoke_paddle_resumed_activated_snapshot_policy.ps1`

## Latest confirmed validation

- `tools/audit_lesson_content.ps1` passed.
- Desktop Debug build passed.
- Desktop Release build passed.
- Backend build passed.
- `dotnet ef migrations list` shows latest confirmed migration `20260618090000_SeedBaseSubscriptionPlans`; latest billing-specific payment persistence migration before base-plan seeding remains `20260529000000_AddPaddlePaymentPersistenceV1`.
- `dotnet ef database update` reports the database is already up to date.
- `dotnet ef migrations has-pending-model-changes` reports no model changes.
- `tools/smoke_paddle_canceled_paused_expiry_policy.ps1` passed.
- `tools/smoke_paddle_cancellation_past_due_policy.ps1` passed.
- `tools/smoke_paddle_entitlement_extension.ps1` passed.
- `tools/smoke_paddle_payment_persistence.ps1` passed.
- `tools/smoke_paddle_subscription_lifecycle.ps1` passed.
- `tools/smoke_paddle_webhook_ingestion.ps1` passed.

### Paddle lifecycle smoke coverage

The Paddle lifecycle smoke scripts verify:

- signed webhook accepted;
- normalization into `billing_events`;
- event-scoped processing for the current provider event;
- subscription snapshot idempotency and out-of-order protection;
- payment snapshot idempotency;
- entitlement activation and extension from validated `reconciliation_pending` billing events;
- scheduled cancellation metadata recording without early revocation;
- past-due snapshot recording without entitlement creation, extension, or revocation;
- actual canceled/paused expiry of only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context;
- backend access/status sees Premium from a `provider_event` entitlement;
- unsigned webhook returns `401`;
- invalid signature returns `401`.

Required local backend configuration for webhook smokes:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

`test_webhook_secret` is a local smoke-test placeholder only. Do not use or commit real Paddle API keys, webhook secrets, price ids, customer ids, or transaction ids.

## Current latest EF migration

- Latest confirmed EF migration is `20260618090000_SeedBaseSubscriptionPlans`.
- `dotnet ef database update` reports the database is already up to date.
- `dotnet ef migrations has-pending-model-changes` reports no model changes.

## Explicit non-goals and deferred scope

Current Paddle billing and entitlement work does **not** complete all production billing operations. Production Paddle readiness planning is tracked in `docs/paddle-production-readiness-checklist.md`; that checklist is documentation-only and does not mark production billing as verified.

Deferred scope / next roadmap:
- Production Paddle webhook setup verification is not completed yet.
- Desktop upgrade/paywall checkout launch with manual refresh exists for sandbox validation; automatic polling remains deferred.
- `subscription.resumed` / `subscription.activated` grace-access restoration before `transaction.completed` is not implemented.
- Refund handling is not implemented yet.
- Chargeback handling is not implemented yet.
- Manual revocation automation is not implemented yet.
- Full provider-neutral subscription reconciliation remains separate; the local Google Play RTDN reconciliation worker is implemented but disabled by default.
- Google Play verification, atomic persistence, authenticated RTDN receipt, protected-token persistence, local reconciliation, and the account-wide purchase gate are deployed but disabled in production. Linked-purchase-token replacement lifecycle handling remains the next local stage; Mobile purchase-gate source is committed separately, but no Mobile store release is claimed. A future Apple App Store adapter is not implemented.
- Production RBAC/admin system is not implemented yet.
- Contabo deployment is not part of this task.

## Current mutation boundaries

- Checkout transaction creation returns `checkoutUrl` only and does not activate Premium.
- Raw webhook ingestion writes `paddle_webhook_events`.
- Normalization writes provider-agnostic `billing_events` with safe metadata only.
- Subscription lifecycle processing updates `SubscriptionEntity` snapshots only; `SubscriptionEntity` does not grant Premium access by itself.
- Payment persistence updates `PaymentEntity` diagnostic snapshots only; `PaymentEntity` is not used for access decisions.
- Reconciliation decision updates only the current normalized provider event decision state.
- Entitlement activation can create or extend Premium `EntitlementEntity` rows from validated `reconciliation_pending` billing events.
- Actual `subscription.canceled` and `subscription.paused` events can shorten only active `provider_event` Premium `EntitlementEntity` rows for the resolved internal user/provider subscription context.
- Trial/manual/admin/development/future-mobile entitlements are not touched by the provider-event canceled/paused expiry path.
- Admin UI was not changed.
- Desktop UI is outside this documentation update; the current desktop upgrade/paywall flow exists for sandbox validation and remains backend-driven.
- Latest payment persistence schema migration is `20260529000000_AddPaddlePaymentPersistenceV1`; latest overall confirmed EF migration is `20260618090000_SeedBaseSubscriptionPlans`.

## Desktop Premium billing controls

Desktop `v0.1.36-tester.24` Account subscription area includes **Buy Premium**, **Cancel subscription**, and **Refresh status** controls. Buy Premium calls the authenticated backend checkout-session endpoint with the `premium` plan. The desktop app opens the backend-hosted Paddle checkout URL in the user's browser and does not call Paddle directly or store Paddle API keys, price ids, webhook secrets, or other private billing secrets.

Checkout creation is not Premium activation. Premium access remains backend-owned and becomes active only from backend entitlement state after Paddle webhook processing. The user must return to the app and use Refresh status after payment so the desktop can read the updated backend state.

The desktop Account subscription area can also request renewal cancellation through the authenticated backend current-user billing endpoint. The backend uses its own subscription snapshot to find the user's Paddle subscription id; the desktop never sends an arbitrary provider subscription id. Cancellation schedules cancel-at-period-end/next-billing-period behavior and does not revoke existing paid Premium entitlements directly. Existing paid Premium access remains available until the entitlement expires or until a future provider lifecycle/refund feature changes that state.

Cancellation state is reflected through subscription snapshot fields such as `CancelAtPeriodEnd`, `ScheduledChangeAction`, `ScheduledChangeEffectiveAtUtc`, `CurrentPeriodEndUtc`, and future Premium/entitlement fields in the current-user subscription status response. Sandbox validation of checkout, webhook activation, paid Premium scheduling after trial, and cancel-renewal remains controlled tester validation and is not a broad production/live Paddle readiness claim.

Desktop and Admin now display backend-computed, provider-neutral renewal/cancellation visibility for controlled tester/sandbox validation. The current-user subscription status response and Admin User Lookup diagnostics expose read-only `renewalStatus`, `nextRenewalState`, `canRequestCancelRenewal`, `cancellationExplanationCode`, cancellation scheduling dates, paid-access dates, and safe provider-event diagnostics from existing subscription snapshot fields. This is UI/diagnostic clarity only: billing authority remains in the backend, Desktop does not infer cancellation locally or call Paddle directly, and no production/live Paddle readiness is implied.

Known follow-ups: Premium-active users should see free lessons as unlimited/no daily free limit instead of “1 remaining” wherever legacy surfaces still show the daily counter; cancellation should be tested end-to-end against Paddle sandbox; referral/promo logic is not implemented and remains future work.

## 2026-06-30 Paddle live checkout guardrails

Language Voice Tutor Pro checkout remains backend-mediated: the desktop app calls backend billing APIs; the desktop app never calls Paddle directly. Backend transaction creation sets Paddle `checkout.url` to `https://languagevoicetutor.com/pay.html`, chooses the configured live Pro price id in live mode without hardcoding it, and includes flat `custom_data` markers `app=language_voice_tutor` and `product=language_voice_tutor_pro` plus the existing backend user/plan markers.

Premium entitlement activation remains provider-agnostic and transaction-completion driven. Webhook processing verifies the Paddle signature before ingestion, records provider events idempotently, and grants/extends Premium only when the expected Pro price id, configured product id, custom_data app/product markers, supported transaction lifecycle, and backend user mapping all match. Subscription snapshot events do not directly grant Premium.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.108` and the 2026-07-02 controlled live payment/cancel-renewal validation:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.1`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- 2026-07-02 controlled validation completed: real live payment Complete for Language Voice Tutor Pro at 14.99 EUR via Google Pay; live checkout transaction creation, `subscription.created`, `subscription.activated`, `transaction.completed`, payment persistence, subscription snapshot processing, reconciliation, entitlement activation (`ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`), and desktop Premium visibility were verified without exposing raw provider payloads or secrets. Earlier failed payment attempts were processed without Premium activation (`ActivatedCount=0` / `AlreadySkippedCount=1`). One PostgreSQL serialization conflict during subscription snapshot processing retried successfully and ended with `FailedCount=0`. Desktop cancel-renewal was verified: auto-renewal became inactive while Premium remained active until `8/2/2026`. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.
- Controlled live payment, webhook delivery, payment persistence, subscription snapshot processing, entitlement activation, desktop Premium visibility, and desktop cancel-renewal behavior were completed and documented on 2026-07-02. Paddle full-refund Premium revocation is production-verified on backend `0.1.35-backend.108` using the already stored live `adjustment.updated` event. Future full refunds should be handled automatically by `adjustment.created` / `adjustment.updated`; the operator reprocess command is reserved for already-stored/legacy events only. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker; broad public paid launch remains pending final release-readiness review and remaining blockers.

Static website upload command must target the real nginx root:

```powershell
scripts/upload-static-site.ps1 -ServerHost "lvt-server" -ServerUser "deploy" -RemotePath "/var/www/languagevoicetutor/site"
```

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, payment/Premium activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation can be reported as completed; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; `billingPaidLaunchReleaseComplete=false` continues until final release-readiness review and remaining blockers are closed.

## 2026-07-02 refund and chargeback Premium protection

In production backend `0.1.35-backend.108`, full Paddle refunds are treated as access-control events after `adjustment.created` or `adjustment.updated` webhook processing: the backend preserves Paddle/payment/subscription history, maps the adjustment back to the internal user by safe metadata or existing payment/subscription records, and expires active provider-event Premium entitlements with reason `paddle_full_refund`. Chargebacks are implemented as stronger refund evidence and are covered by tests/fake paths, but no real live chargeback was performed.

Normal cancel-renewal behavior is unchanged: scheduled cancellation keeps Premium through the paid period end. Partial refunds are conservative in this slice: the event is safely recorded/processed for review and Premium is left unchanged unless the adjustment is full or a chargeback. Provider history is preserved; payment and subscription records are not deleted, and refund processing does not fake Paddle webhook events or expose raw provider payloads, webhook signatures, tokens, cookies, secrets, API keys, or full card/payment data in Admin Activity evidence.

Full-refund Premium revocation is production-verified on current production backend `0.1.35-backend.108`: the operator reprocess of stored provider event `evt_01kwhgmvh1v9k8ve70gvnfeskm` returned `Result=Revoked`, `RevokedCount=1`, and `BlockReason=(null)`; Admin User Lookup confirmed Free/no Premium/no Trial; Admin Activity showed `paddle_full_refund_premium_revoke` succeeded for the refunded user. Broad public paid launch is no longer blocked by full-refund revoke, but remains pending final release-readiness review and remaining blockers. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.


### 2026-07-02 production refund replay blocker and fix candidate

Production backend `0.1.35-backend.108` is deployed and healthy. Paddle has `adjustment.created` and `adjustment.updated` enabled and delivered. Future full refunds should be handled automatically by those adjustment notifications. The operator reprocess command remains for already-stored/legacy events only.

For full-refund/chargeback adjustment events, use safe metadata first, then resolve the backend user from existing Paddle payment history by provider transaction id, then existing subscription history by provider subscription id, then active provider-event entitlement evidence where already linked. If no safe mapping exists, block with a safe reason. No more live payment/refund/replay testing is required for this release-readiness slice. Broad public paid launch remains pending final release-readiness review and remaining blockers. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.

Production verification after deployment must not create another live payment or refund: replay the already delivered Paddle `adjustment.updated` notification from the Paddle notification log, then refresh Desktop/Admin subscription status and confirm Premium is removed.

## Paddle adjustment recovery/reprocess path

The backend includes an explicit operator-only recovery path for already-stored Paddle `adjustment.created` / `adjustment.updated` billing events. It exists for one-off recovery when an adjustment event was safely stored but was previously skipped or blocked before entitlement revocation, such as the production `.96 -> .97` full-refund replay case. The path accepts a specific provider event id, reuses the existing `billing_events` row, refuses non-adjustment event types, does not create a fake Paddle webhook event, does not delete provider history, and verifies that `PaymentEntity` and `SubscriptionEntity` row counts are unchanged.

Backend `.98` proved the operator command can find the stored `adjustment.updated` event `evt_01kwhgmvh1v9k8ve70gvnfeskm`, resolve the user through `PaymentEntity`, detect a full refund, and find one active provider-event Premium candidate, but it still returned `Result=Blocked` / `BlockReason=reconciliation_blocked` because reprocess still depended on the old blocked reconciliation state. Backend `.99` changes only the explicit operator-only recovery path: after the event type, full-refund/chargeback, safe user mapping, and active provider-event Premium checks pass, it bypasses old reconciliation state and directly invokes the existing safe adjustment revocation logic. Full refunds and chargebacks revoke active provider-event Premium; partial refunds are conservative and do not automatically revoke Premium. The operation is idempotent: if Premium is already revoked or no active provider-event Premium entitlement remains, the result is `AlreadyRevoked`; unknown event ids return `NotFound`; unsafe mapping returns a blocked result; and non-`adjustment.created`/`adjustment.updated` events are refused.

Production note: backend `.97` duplicate replay was idempotent, `.98` operator reprocess was blocked by old reconciliation state, and `.99` operator reprocess of `evt_01kwhgmvh1v9k8ve70gvnfeskm` returned `Result=Revoked`. Premium is inactive for the refunded user, provider history remains present for diagnostics, and no more live payment/refund/replay testing is required for this release-readiness slice. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.
