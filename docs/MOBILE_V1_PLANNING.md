# Mobile v1 Planning

Review date: 2026-07-05.

## Progress contract

Authenticated Progress V1 is available at `GET /api/me/progress`. Mobile consumes this backend-owned aggregate contract and must not calculate official totals or streaks from the maximum-50 History list. See [Progress Endpoints](PROGRESS_ENDPOINTS.md).

This document prepares safe Mobile v1 planning for Language Voice Tutor. It is analysis-only and documentation-only. It does not create a mobile app project, select a final technology, change runtime behavior, change billing behavior, add database migrations, or change production deployment artifacts.

## Product principle

Mobile v1 is the same **Language Voice Tutor** product, not a separate mobile product.

Mobile is another client for the same backend-owned account, database, subscription, entitlement status, lesson history, progress, usage limits, and AI tutor behavior. A learner should be able to move between desktop and mobile and see the same account state after the client checks the backend.

Mobile UI may differ only because phone screens, touch input, mobile keyboards, and phone voice interaction require a different layout and interaction model. The product identity, account model, lesson model, Premium model, limits, and AI tutor behavior must stay shared.

Required shared-product boundaries:

- No mobile-only backend.
- No mobile-only database.
- No mobile-only account system.
- No mobile-only Premium state.
- No mobile-only OpenAI call path.
- No client-side Premium decisions.
- No client-side entitlement authority.

## Mobile v1 scope

Mobile v1 should plan for these shared capabilities:

- Same backend authentication, registration, login, logout, and session behavior as desktop.
- Same user account as desktop and website checkout flows.
- Same Premium, subscription, entitlement, trial, cancellation, pause, expiration, and revocation status after backend account checks.
- Same Free/Premium lesson limits and backend-owned enforcement.
- Same lesson history and progress model.
- Same study language, level, topic, and scenario model.
- Same AI tutor lesson behavior and backend-owned prompt/model behavior.
- Mobile text mode for stable lesson chat when the backend lesson APIs support it.
- Mobile voice mode when recording, upload/transcription, assistant response, and playback/TTS contracts are stable enough for mobile reliability.
- Mobile account/settings screens where the same backend settings/profile data is applicable.
- Mobile diagnostics/app-version metadata only where it helps support, abuse prevention, compatibility, and release triage.

## Explicitly out of scope for this planning step

The following are not part of this documentation-only planning step:

- No mobile implementation code yet.
- No Flutter, React Native, Kotlin, .NET MAUI, Android, or iOS project creation yet.
- No App Store or Google Play public release plan yet.
- No separate mobile backend, database, account system, subscription model, or Premium state.
- No client-side OpenAI calls.
- No client-side Premium decisions.
- No OpenAI, Paddle, Google Play, Apple, webhook, database, JWT, or other secrets in mobile apps.
- No CMS-owned website text edits through code.
- No Microsoft Store/MSIX return as the main release path.
- No Paddle runtime behavior changes.
- No backend runtime code changes.
- No desktop app code changes.
- No website/CMS runtime code changes.
- No database migrations.
- No installer, release artifact, or production deployment script changes.

## Backend/API gap analysis

Mobile should not assume desktop UI internals are enough. Before implementation, the backend contract should be reviewed as a stable cross-client API.

### Auth and session requirements

Mobile needs a clear contract for:

- Register, login, logout, token/session refresh, session expiry, and device sign-out behavior.
- How the client detects expired sessions without losing local unsaved UI state.
- Error codes for invalid credentials, locked/disabled accounts, rate limits, unverified state if added later, and session expiration.
- Secure token storage expectations for mobile platforms.
- Whether multiple active devices are allowed and how suspicious sessions are reported or revoked.

### `/api/me` and settings sync requirements

Mobile needs a stable account summary and settings sync contract for:

- Current account identity and safe display fields.
- Premium/entitlement summary suitable for learner UI.
- Trial/free/Premium status and relevant dates.
- Free lessons remaining or limit display fields.
- Study language, level, topic, scenario, voice, UI, selected tutor, and lesson preferences where shared. Mobile can now read tutor options from `GET /api/tutor-options` and, in a later mobile-only implementation task, persist `selectedTutorId` through `GET`/`PUT /api/me/settings`.
- Server timestamps and version fields so mobile can reconcile cached settings safely.
- Safe partial updates for mobile settings without overwriting desktop-only values.

### Subscription status and lesson access requirements

Mobile needs backend-owned access decisions for:

- Whether a new lesson can start now.
- Why lesson start is blocked, if blocked.
- Whether the user is active Trial, active paid Premium, free with allowance, expired, paused, canceled, refunded, chargebacked, or in another support state.
- A safe learner-facing subscription summary that does not expose provider secrets or raw webhook diagnostics.
- Consistent desktop/mobile behavior after every backend account status check.

### Lesson start, message, history, and progress requirements

Mobile needs stable DTOs and errors for:

- Starting a lesson with study language, level, topic, scenario, and optional voice/text mode.
- Sending user messages and receiving assistant messages.
- Continuing an existing lesson session.
- Fetching recent lessons, full lesson history, lesson summaries, and progress.
- Handling network interruptions, retries, duplicate sends, and idempotency.
- Distinguishing validation errors, entitlement/access errors, rate limits, AI provider failures, and transient server errors.

### Voice upload and TTS contract requirements

Mobile voice mode requires explicit contracts for:

- Supported audio formats, sample rates, duration limits, file size limits, and upload transport.
- Transcription request/response DTOs and error codes.
- Whether voice upload is tied to a lesson session/message id for idempotency.
- TTS response format, streaming versus non-streaming playback, caching rules, and retry behavior.
- Permission-denied microphone flows and fallback to text mode.
- Rate limits and abuse protections for recording upload, transcription, TTS, and lesson messages.
- Privacy wording alignment with existing AI/data disclosures.

### Device, platform, and app-version tracking

Mobile should send safe metadata that helps support and compatibility without becoming a new account source of truth:

- Platform: Android first if chosen later, future iOS when approved.
- App version/build number and API contract version.
- Device class or OS version where useful, avoiding invasive identifiers.
- Install/session identifiers only if privacy-reviewed and support/abuse use is clear.
- Last-seen and diagnostics data should remain backend-owned and should not be treated as raw download counts.

### API versioning and stable DTO/error contract

Before mobile implementation, the backend should define:

- Which endpoints are stable for mobile v1.
- Whether mobile uses existing endpoints, versioned routes, or explicit API contract versions.
- Stable request/response DTOs for account, settings, subscriptions, lessons, history, progress, voice, and TTS.
- Stable error code names and HTTP status usage.
- Backward compatibility expectations when desktop and mobile app versions differ.
- Deprecation policy for client-visible fields.

### Usage and rate-limit requirements

Mobile voice and lesson usage need cross-client limits that remain backend-owned:

- Lesson-start limits must match the same Free/Premium model used by desktop.
- Message, transcription, TTS, and voice upload limits should account for mobile retry behavior and unstable networks.
- Rate-limit responses should be user-safe and machine-readable.
- Abuse protection should not expose provider keys, provider internals, or sensitive scoring data.
- Limits must be computed by backend account/entitlement state, not by local mobile checks.

## Billing provider plan

### Google Play purchase-verification foundation

`POST /api/me/billing/google-play/purchases/verify` is an authenticated backend boundary. Its request contains only `purchaseToken`; the authenticated account is derived from access-token claims. Production currently registers a disabled verifier and returns `503 not_configured` with no entitlement change. There is no Google API, credential, purchase-token persistence, acknowledgement, RTDN, deployment, or Mobile production integration yet. Mobile must not call this route in production until a later approved live-verification and entitlement slice.

A Google Play purchase-claim table now stores only an irreversible SHA-256 token fingerprint, allowing one verified purchase to be bound to one LVT account without storing the raw token. It prevents cross-account attachment while the production verifier remains disabled. There is still no Google API, Premium activation, acknowledgement, Mobile connection, RTDN, or deployment.

A future provider-verified purchase must pass ownership claiming before it can return `verified`: same-account retries return `already_processed`, while cross-account reuse returns `ownership_conflict`. Production verification remains disabled; no Google API, Premium activation, acknowledgement, Mobile connection, RTDN, migration application, or deployment exists.

A dormant subscriptions-v2 verifier implementation now validates disabled-by-default package configuration, exact allowed ProductIds, provider state, and line items through a sanitized Google-client boundary. Production still uses the disabled verifier; credentials and live registration remain absent. linkedPurchaseToken reconciliation, lifecycle reconciliation, acknowledgement, Premium activation, Mobile connection, RTDN, migration application, and deployment remain pending.

Paddle remains valid for website and desktop checkout. Mobile billing may need a different provider because app stores have their own payment rules, but all providers must map into one backend entitlement source of truth.

Required billing direction:

- Paddle remains valid for website/desktop.
- Android billing must use Google Play Billing / Google Play payment flow when mobile billing is implemented for Google Play distribution.
- The mobile app may initiate a Google Play purchase and send the purchase token to the backend.
- The backend must verify Google Play purchases using the Google Play Developer API before granting Premium.
- Google Play purchase, renewal, cancellation, pause, expiration, refund, chargeback/revocation, and restore events must map into the same backend entitlement model used by Paddle and desktop.
- A future Apple App Store provider must map into the same backend entitlement model.
- Premium paid through Paddle must be visible on mobile after backend account status check.
- Premium paid through Google Play must be visible on desktop after backend account status check.
- Mobile clients must not store billing secrets and must not decide Premium locally.
- Provider-specific subscription snapshots are diagnostics/support inputs; active Premium access remains entitlement-owned.

Open billing decisions:

- Whether Android v1 includes in-app purchase at launch or initially displays account/Premium status only.
- Whether account creation and Paddle-paid Premium from the website are sufficient for an early private mobile test.
- Exact Google Play product ids, base plans, offers, restore flow, and backend reconciliation jobs.
- Apple App Store timing and whether iOS follows Android after the shared entitlement bridge is proven.

## Technology options, not final selection

No implementation technology is selected by this document. Any recommendation below is provisional and must wait for Mobile v1 scope approval, backend/API gap review, billing-provider plan approval, and owner approval.

| Option | Fit for this product | Strengths | Risks / questions |
| --- | --- | --- | --- |
| Flutter | Strong cross-platform candidate for Android first with future iOS. | Fast UI iteration, mature mobile ecosystem, good audio/plugin options, single codebase for future iOS, strong custom layouts for lesson chat and voice UI. | Requires Dart/Flutter expertise, plugin vetting for recording/playback/background behavior, native billing bridge still needed, desktop code is not reused directly. |
| React Native | Strong cross-platform candidate if JavaScript/TypeScript mobile velocity is preferred. | Large ecosystem, good UI velocity, many auth/audio/subscription libraries, future iOS path. | Native module/plugin quality varies, audio edge cases can require native code, dependency churn risk, billing and secure storage need careful review. |
| Kotlin Multiplatform / Android native first | Strong Android-first candidate when Google Play Billing, audio reliability, and native platform behavior are highest priority. | Best Android integration, direct Google Play Billing support, strong audio/permissions control, Kotlin Multiplatform can share some future business/client API code. | Slower path to iOS UI parity unless KMP sharing is carefully scoped, Android-first may defer iOS design decisions, more native-platform implementation effort. |
| .NET MAUI | Candidate if reuse of .NET/C# skills is prioritized. | C#/.NET familiarity, possible shared DTO/client library direction, cross-platform ambition. | Mobile ecosystem and plugin maturity must be validated for audio, billing, app-store requirements, and long-term maintainability; may not be fastest or lowest-risk for polished phone voice UX. |

Provisional planning recommendation: treat Flutter and Kotlin/Android-native-first as the leading options to compare in a later decision document, with React Native and .NET MAUI still considered until voice/audio, billing, team skill, and future iOS needs are reviewed. This is not approval to create a mobile project.

## Open questions and decisions

- Approve the Mobile v1 scope and out-of-scope boundaries.
- Decide whether Android v1 starts as private/internal testing, closed testing, or another non-public validation path before any public store plan.
- Decide whether Mobile v1 must include voice mode at first release or can start with stable text mode plus backend-backed history/progress.
- Review backend/API gaps and decide which endpoints need contract hardening before mobile implementation.
- Review Google Play Billing entitlement bridge requirements and whether mobile billing is needed for the first mobile validation build.
- Choose mobile technology only after scope, backend/API gaps, and billing provider plan are approved.
