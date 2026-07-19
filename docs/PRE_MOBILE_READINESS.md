# Pre-Mobile Readiness

Review date: 2026-07-19.

This note is a concise planning input for future mobile work. It records the current shared product baseline only. It is not a mobile architecture plan, mobile UI plan, framework choice, App Store plan, Google Play plan, or implementation checklist. For the current Windows client functionality baseline that mobile should reuse or mirror, see [Windows Client Functionality Overview](WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md).

## Current release baseline

- Windows Direct Release `1.1` is published and verified.
- Public Windows release channel is `direct-public`.
- Public installer is `LanguageVoiceTutorSetup-1.1.exe`.
- Public direct manifest remains `https://languagevoicetutor.com/releases/windows/direct/latest.json` and must be verified over HTTPS before using it as live evidence.
- Packaged Windows release builds use backend URL `https://api.languagevoicetutor.com`.
- Desktop update mode is `manual-confirmation`; the app checks the manifest, asks before download/install, and does not silently auto-update.
- Desktop app version `1.1` works correctly, including the desktop auth/session disconnect regression fix.
- Mobile apps are planned but are not currently available.

## Current backend baseline

- Production backend is `0.1.35-backend.123` and healthy at `https://api.languagevoicetutor.com`.
- Backend health and database health are expected to be verified with `/health` and `/api/health/database` before treating the backend as current; the previous release for the 2026-07-19 state is `0.1.35-backend.122`.
- OpenAI calls are backend-only. Desktop clients call backend APIs; future mobile clients must do the same.
- Website analytics is working, including the fixed `pay.html` analytics/consent coverage.
- Public website pages no longer show tester wording.
- `site/public/llms.txt` has already been updated to remove tester/pre-live Paddle wording.
- Website public text is CMS-owned unless a file is explicitly not CMS-managed, such as `llms.txt`.

## Stable planning inputs

These facts are stable enough to use as mobile planning inputs:

- One production backend already owns accounts, auth/session behavior, subscription/Premium entitlement state, usage/limits, lesson history/progress, and user settings/profile data where applicable.
- Windows desktop is already integrated with the production backend for registration/login, lessons, history/progress, Premium visibility, and update metadata.
- Desktop auth/session disconnect behavior was corrected in Windows Direct Release `1.1`, so mobile planning should preserve the backend-owned session model rather than inventing a separate client-specific account model.
- Website CMS owns public website wording; repository static files can be stale snapshots unless they are explicitly non-CMS-managed files.
- Backend deploy, static site upload, Windows release upload, and database migrations are separate operations and must remain separate in planning and runbooks.

## Mobile v1 product principle

Mobile v1 is another client for the same Language Voice Tutor product, not a separate product. The first mobile version must include the same core product functionality as the Windows desktop app, adapted visually and ergonomically for phone screens. Users should recognize the same account, learning model, Premium status, and lesson behavior rather than feeling like they moved to a different product.

## Mobile v1 shared product scope

Future mobile work must preserve the same product model across Windows desktop and mobile:

- Same user account as Windows desktop.
- Same production backend.
- Same backend database.
- Same Premium, subscription, and entitlement status.
- Same usage and limits model.
- Same lesson history and progress.
- Same backend-owned lesson completion and summary source of truth.
- Same study-language, level, topic, and scenario model.
- Same AI tutor lesson behavior, adapted to mobile UX and phone ergonomics.
- Same account, settings, and profile model where applicable.
- Mobile UI should be visually adapted for phone screens, but product behavior should remain consistent with desktop.

## Shared backend, account, and entitlement boundary

The backend remains the source of truth for accounts, auth/session behavior, Premium entitlement, usage/limits, lesson history/progress, settings/profile data where applicable, and AI tutor requests. Windows desktop and mobile clients must check the same backend account status and must not maintain client-specific entitlement decisions.

Required shared-boundary rules:

- No separate mobile backend.
- No separate mobile database.
- No separate mobile account system.
- No separate mobile-only Premium state.
- No client-side OpenAI calls.
- No client-side Premium decisions.
- No OpenAI keys, Paddle secrets, Google Play credentials, Apple credentials, webhook secrets, or billing secrets in mobile clients.

## Billing provider and payment verification boundary

Payment provider may differ by purchase surface, but Premium entitlement must remain shared through the backend entitlement/source-of-truth model. Existing Paddle billing remains valid for website/desktop. A future Google Play Billing provider should plug into the backend as another billing provider, and a future Apple App Store provider may later plug into the same backend entitlement model.

Android payments should be planned around Google Play Billing, not a separate client-side Google Pay-only entitlement model. The mobile app may initiate the Google Play purchase flow and send the resulting purchase token to the backend, but the backend must verify the purchase with the Google Play Developer API before Premium is granted. After verification, the backend creates, extends, pauses, expires, or revokes Premium through the same entitlement/source-of-truth model already used by desktop/Paddle.

Cross-client Premium recognition must remain account/backend based:

- Desktop must recognize Premium purchased through Google Play after checking backend account status.
- Mobile must recognize Premium purchased through Paddle, website, or desktop after checking backend account status.
- Do not create separate mobile subscriptions outside the backend entitlement model.
- Do not let the mobile client decide Premium locally.

## Current mobile lesson completion integration step

Authenticated mobile Finish lesson + ready Summary is production-verified as of 2026-07-11 without a new backend endpoint by using `PUT /api/me/lesson-sessions/{sessionId}/finish` and `GET /api/me/lesson-sessions/{sessionId}/summary`. The mobile client must not generate summaries locally or upload summary fields; the backend generates and persists summaries from persisted lesson messages and safe metadata. Finish triggers backend-owned generation. GET only reads the stored learner-safe result, can return `ready` learner-safe fields or `unavailable`, and does not regenerate a missing summary. Development `/api/dev/.../summary` routes remain diagnostic-only and are not production mobile contracts.

## Authenticated Lesson History client boundary

Backend `0.1.35-backend.123` completes and production-deploys authenticated recent Lesson History. Mobile can use `GET /api/me/lesson-history` for the newest-first recent list (currently at most 50) and `GET /api/me/lesson-history/{sessionId:guid}` for owned metadata, summary, transcript messages, and feedback. Both routes require authentication; ownership is derived from the authenticated identity, never a client-supplied user ID. Unknown and non-owned detail requests safely return `404`. The canonical response contract is [Lesson History Endpoints](LESSON_HISTORY_ENDPOINTS.md).

Mobile UI consumption remains separate client work. Mobile must not use Desktop-local JSON or `/api/dev/...` history endpoints. The recent History list is not all-time Progress and must not be used to calculate official totals, streaks, aggregates, or long-term statistics. A future official Progress feature needs a separate backend-owned aggregate contract; clients must not invent it locally.

## Explicit constraints

Mobile planning must not introduce or assume:

- A separate mobile backend.
- A separate mobile database.
- A separate mobile account system.
- A separate mobile subscription model.
- A separate mobile-only Premium state.
- Client-side OpenAI calls.
- Client-side Premium decisions.
- Mobile implementation code in this task.
- Mobile architecture planning in this document.
- Mobile UI framework selection in this document.
- App Store or Google Play release planning in this document.
- Billing code changes in this document.

## Known risks and backlog for mobile planning

Mobile planning must not forget these existing product/release risks:

- Code signing / SmartScreen remains a Windows trust issue for the direct installer path.
- Customer portal work is deferred.
- Chargeback handling is implemented and test-covered but has not been live-chargeback-tested.
- Partial refund handling remains manual/conservative review.
- Broad paid launch remains pending final readiness/legal/support/ops review.
- Static upload can overwrite CMS-published analytics configuration if used carelessly.
- Website CMS owns public website text; avoid editing CMS-owned public copy directly in repository snapshots.
- Backend deploy, static site upload, Windows release upload, and database migrations are separate operations.

## Future documentation cleanup candidates only

Do not archive or delete documents as part of this note. Future cleanup may identify older Windows `1.0`, backend `.99`, tester-era, or pre-live Paddle documents as archive candidates, but that should be a separate reviewed documentation task.


## 2026-07-11 verified mobile summary boundary

Production backend `0.1.35-backend.112` fixes the authenticated lesson-summary provider-output extraction issue found in `0.1.35-backend.111`: top-level Responses API `output_text` remains supported, nested `output[].content[].text` is supported, blank provider output is rejected before JSON deserialization, and summary failure remains isolated from successful lesson completion. No client/local summary generation was added.

The 2026-07-11 production verification used a real authenticated Flutter mobile lesson and confirmed session start, message persistence, Finish completion, and a ready backend-owned Summary displayed by mobile. This verifies only authenticated mobile Finish + ready Summary. It does not complete Mobile History UI, aggregate Progress, voice, translation, hints, feedback, TTS, Conversation mode, billing, store publication, or broad public production readiness. Authenticated recent History was completed separately in backend `0.1.35-backend.123`. Authenticated desktop Finish uses the shared completion path, but desktop currently displays its existing local desktop summary flow and `.112` does not change desktop UI or Finish response contracts.
