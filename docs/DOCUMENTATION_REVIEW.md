# Documentation Review

Review date: 2026-06-01.

## What was reviewed

The documentation sync reviewed repository docs and release-relevant scripts/code paths for:

- current README and state/roadmap docs;
- local and tester release docs;
- desktop release smoke gate, work plan, and readiness audit;
- billing foundation and production billing planning docs;
- CMS/Admin planning docs;
- auth/session storage implementation;
- active lesson heartbeat/remote-release implementation and smoke script;
- package tester script and release gate script;
- EF migration files;
- current audit/smoke scripts.

## What was updated

Docs were synchronized to record the current accepted state:

- `scripts/package-tester-release.ps1` is the canonical current tester ZIP flow.
- Default tester ZIP is `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`.
- The tester ZIP was verified on another Windows device after extraction.
- Packaged Release hides Diagnostics by default and uses local `EVT_DESKTOP_DIAGNOSTICS=1` only for support/testing.
- Core Lesson Chat, Conversation Mode, TTS, transcription, translation, hints, feedback, and summary are accepted for the current controlled desktop MVP.
- Desktop auth session storage uses a Windows DPAPI-protected local `auth-session.json` payload, not raw plaintext token JSON.
- Active lesson protection is backend-enforced, heartbeat-based, supports remote release, marks old sessions `Abandoned`, and rejects old heartbeat/message actions.
- Latest confirmed EF migration is `20260601090000_AddLessonSessionHeartbeat`.
- Current smoke/audit scripts include the desktop release gate, lesson/localization/backend-boundary audits, single active lesson guard smoke, Paddle smokes, and Admin smokes.

## Intentionally deferred documentation topics

- Production billing remains deferred until desktop hardening is complete.
- CMS/Admin operational readiness remains deferred until desktop hardening is complete.
- Prompt/scenario/dialogue/bot-behavior quality polishing is deferred to CMS/Admin.
- Public release is not declared ready.
- Installer/signing/Microsoft Store packaging is not documented as complete.
- Mobile app implementation and mobile entitlement bridge are not documented as complete.

## Future documentation updates needed

Update documentation again after any of these future events:

- production backend URL/configuration is approved for a broader release;
- production Paddle billing is configured and manually smoke-tested;
- refund/chargeback/revocation/reconciliation behavior is implemented;
- CMS/Admin operational scope is approved or implemented;
- installer/signing/update/distribution path is selected;
- Study languages, Interface languages, or Native/Explanation catalog changes are explicitly approved.
