# Pre-Mobile Readiness

Review date: 2026-07-04.

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

- Production backend is `0.1.35-backend.108` and healthy at `https://api.languagevoicetutor.com`.
- Backend health and database health are expected to be verified with `/health` and `/api/health/database` before treating the backend as current.
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
