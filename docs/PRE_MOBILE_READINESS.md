# Pre-Mobile Readiness

Review date: 2026-07-04.

This note is a concise planning input for future mobile work. It records the current shared product baseline only. It is not a mobile architecture plan, mobile UI plan, framework choice, App Store plan, Google Play plan, or implementation checklist.

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

## Shared product model required for Windows and mobile

Future mobile work must preserve a single shared product model across Windows desktop and mobile:

- One backend account.
- One backend database.
- One subscription/entitlement state.
- One usage/limits model.
- One lesson history/progress source.
- One settings/profile model where applicable.

## Explicit constraints

Mobile planning must not introduce or assume:

- A separate mobile backend.
- A separate mobile subscription model.
- Client-side OpenAI calls.
- Client-side Premium decisions.
- Mobile architecture planning in this document.
- App Store or Google Play implementation planning in this document.

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
