# Desktop Release Work Plan

Review date: 2026-05-31

Status:
Implementation note added after Step 5B-1 desktop Settings acceptance and Diagnostics Release gate work.

## Source documents reviewed

- `docs/desktop-release-readiness-audit.md`
- `docs/NEXT_STEPS.md`
- `docs/CURRENT_STATE.md`
- `docs/desktop-upgrade-paywall-ui-plan.md`
- `docs/paddle-production-readiness-checklist.md`
- `docs/billing-remaining-operations-plan.md`
- `docs/CMS_ADMIN_PLANNING.md`

## Current conclusion

- The desktop product is close enough for focused internal validation, but it is not public-release-ready yet.
- A controlled tester release is possible only after the P0/P1 hardening work is completed or explicitly accepted for a limited internal audience.
- Production billing is not the next active focus. Paddle production rollout remains deferred until desktop release hardening is complete.
- CMS/Admin is not the next active focus except for a later operational audit after desktop readiness and minimum support requirements are clear.
- Backend must remain the source of truth for account, trial, subscription, Premium/free status, usage, limits, lesson history, payments, entitlements, and user settings.
- Desktop must continue to avoid local Premium decisions, real secrets, direct OpenAI calls, and OpenAI API key storage.

## Updated priority order

### Phase 5B — Desktop release hardening

1. Step 5B-1: Settings final acceptance and Diagnostics Release gate
2. Step 5B-2: Native languages and localization foundation
3. Step 5B-3: Backend unavailable and Account UX hardening
4. Step 5B-4: Auth session storage production decision
5. Step 5B-5: Lesson selection and access-state QA
6. Step 5B-6: Lesson Chat MVP polish
7. Step 5B-7: Voice recording/transcription reliability pass
8. Step 5B-8: Bot voice/TTS loading, failure, and avatar-state acceptance
9. Step 5B-9: Conversation Mode MVP acceptance or beta/hide decision
10. Step 5B-10: Avatar framing/profile/prompt acceptance pass
11. Step 5B-11: Lesson completion, early exit, summary, and progress manual test
12. Step 5B-12: Free-limit/paywall desktop UX acceptance without billing logic changes
13. Step 5B-13: Release diagnostics/config cleanup and copied-output safety check
14. Step 5B-14: Release build config checklist and production backend URL decision
15. Step 5B-15: Installer/signing/clean-machine release package checklist
16. Step 5B-16: Human lesson methodology/content sample review
17. Step 5B-17: CMS/Admin operational readiness audit after desktop hardening
18. Step 5B-18: Desktop security/privacy release checklist
19. Step 5B-19: Run and record manual desktop release checklist results
20. Step 5B-20: Final P0/P1 triage

### Phase 5C — Production billing readiness

Keep production Paddle rollout after desktop release hardening.

Phase 5C should use the existing production billing planning/checklist documents and must not be treated as enabled until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely outside tracked files and without committing secrets.

### Phase 5D — CMS/Admin operational readiness

Keep CMS/Admin after desktop hardening, with read-only support/admin first.

Phase 5D should start with the minimum operational support needs for a desktop release: account lookup, support diagnostics, entitlement visibility, free-limit support, audit trail, refund/chargeback visibility, and content hotfix workflow. Full CMS content authoring, draft/published lifecycle, prompt editing, and broad production RBAC remain later work.

## Phase 5B work items

### Step 5B-1: Settings final acceptance and Diagnostics Release gate

Purpose:
- Confirm Settings is stable, understandable, and organized for release validation.
- Decide whether Diagnostics is hidden/default-off in Release builds or deliberately enabled only for support/tester builds.
- Verify copied diagnostics output does not expose secrets, tokens, raw audio paths, raw lesson-history content, payment secrets, OpenAI API keys, or environment variables.

Acceptance criteria:
- Settings opens reliably in Debug and Release.
- Learning, Account, Audio, Progress, and Diagnostics separation is accepted.
- Release Diagnostics visibility policy is documented and implemented in a later implementation step if needed.
- Copied diagnostics output is manually reviewed and accepted as safe.

Implementation note (2026-05-31):
- Step 5B-1 has been implemented in desktop Settings: Diagnostics remains available by default in Debug builds, is hidden by default in Release builds, and can be enabled for Release support sessions with the explicit local `EVT_DESKTOP_DIAGNOSTICS` flag. Copied diagnostics output now masks common secret/token/key values and strips URL user-info, query strings, and fragments before display/copy.

### Step 5B-2: Native languages and localization foundation

See [Native languages and localization foundation](#native-languages-and-localization-foundation) for the detailed plan.

### Step 5B-3: Backend unavailable and Account UX hardening

Purpose:
- Make stopped backend, wrong backend URL, timeout, expired/invalid token, and 401/403/500 flows understandable.
- Validate Register, Login, Logout, Settings sync, account status refresh, lesson access preflight, chat, hints, translation, transcription, TTS, checkout launch, and manual Refresh status when the backend or account state is not usable.

Acceptance criteria:
- Backend-down and wrong-URL errors do not crash the app.
- Account-required and free-limit states are clear to new users.
- Expired/invalid session behavior is accepted.
- Retry guidance is understandable.

### Step 5B-4: Auth session storage production decision

Purpose:
- Decide whether local JSON auth session storage is acceptable only for controlled testers or must be replaced before public release.
- Document or implement a secure Windows-backed token storage approach in a later implementation step.

Acceptance criteria:
- Public-release token-storage decision is documented.
- Migration/cleanup behavior for existing tester sessions is defined if storage changes.
- No token or secret is included in release packages, diagnostics output, docs, or committed files.

### Step 5B-5: Lesson selection and access-state QA

Purpose:
- Validate the complete lesson start path across signed-out, signed-in free available, free used, trial, Premium, past-due, canceled/paused, checkout unavailable, and unknown/error states.

Acceptance criteria:
- Lesson selection remains understandable in each account/access state.
- Desktop does not make local Premium decisions.
- Backend access/status remains authoritative.

### Step 5B-6: Lesson Chat MVP polish

Purpose:
- Review the first message, typed sends, command states, disabled controls, status copy, Hint, Translate, Play voice, Finish, Back, and completed-awaiting-finish UX.

Acceptance criteria:
- Core lesson chat path is understandable without developer knowledge.
- Failure states provide useful retry guidance.
- No debug-only text leaks into normal release UX.

### Step 5B-7: Voice recording/transcription reliability pass

Purpose:
- Test microphone selection, System default, missing devices, changed default device, unavailable saved microphone, permission denial, start/stop behavior, poor input, backend transcription failure, and auto-send voice behavior.

Acceptance criteria:
- Voice path works on clean Windows tester machines.
- Missing/denied microphone states are understandable.
- Transcription failures do not crash the lesson.

### Step 5B-8: Bot voice/TTS loading, failure, and avatar-state acceptance

Purpose:
- Verify Play voice and automatic bot voice behavior across Lesson Chat and Conversation Mode.
- Confirm TTS loading states, failures, retry behavior, and avatar animation/state are understandable.

Acceptance criteria:
- Bot voice failures do not block text lesson progress.
- Avatar and loading state do not misrepresent a failed or pending bot response.
- Desktop uses backend TTS APIs only.

### Step 5B-9: Conversation Mode MVP acceptance or beta/hide decision

Purpose:
- Decide whether Conversation Mode is accepted for controlled MVP, labeled as beta, or hidden for first external release.
- Validate avatar layout, record UX, auto-send, auto-play, Hint, return/back, lesson completion, and common window sizes.

Acceptance criteria:
- Conversation Mode behavior is accepted for the target audience, or a hide/beta decision is documented.
- Small and medium window layouts are manually accepted.
- Voice and TTS failure states are acceptable.

### Step 5B-10: Avatar framing/profile/prompt acceptance pass

Purpose:
- Review Elena and Nelli visual framing, tutor profiles, prompt behavior, and personality balance.

Acceptance criteria:
- Avatar behavior supports the lesson goal and does not distract from learning.
- Prompt/profile behavior passes a human acceptance sample.
- MVP marketing does not overstate avatar breadth.

### Step 5B-11: Lesson completion, early exit, summary, and progress manual test

Purpose:
- Validate Finish lesson, completed-awaiting-finish, Back/early exit, saved summaries, History, statistics, and progress updates.

Acceptance criteria:
- Typed turns, voice turns, invalid transcript retries, and Conversation Mode turns behave correctly.
- Summary failure does not block the user inappropriately.
- Early exit behavior is accepted or clarified with copy in a later implementation step.

### Step 5B-12: Free-limit/paywall desktop UX acceptance without billing logic changes

Purpose:
- Validate desktop free-limit, access denied, Upgrade, checkout launch, checkout unavailable, checkout failed, and manual Refresh status UX without changing billing logic.

Acceptance criteria:
- Desktop upgrade/paywall flow remains backend-driven.
- No production Paddle rollout is performed in this phase.
- No local Premium decision is added.
- Copy does not promise Premium until backend state reports Premium.

### Step 5B-13: Release diagnostics/config cleanup and copied-output safety check

Purpose:
- Verify Release diagnostics/config behavior, copied-output safety, backend URL exposure, and support-only diagnostics expectations.

Acceptance criteria:
- Normal Release users do not see diagnostics unless deliberately enabled.
- Copied diagnostics output contains no real secrets or sensitive payloads.
- Backend URL/config expectations are documented for the target release type.

### Step 5B-14: Release build config checklist and production backend URL decision

Purpose:
- Define explicit Release build expectations and decide how the desktop release points to the intended backend without storing secrets.

Acceptance criteria:
- Desktop Debug and Release builds pass.
- Production/test backend URL strategy is documented.
- Release package contains no backend source, local settings/history files, API keys, payment secrets, tokens, or environment-specific secrets.

### Step 5B-15: Installer/signing/clean-machine release package checklist

Purpose:
- Decide zip versus installer, signing, clean-machine test, versioning, download, update, rollback, and support expectations.

Acceptance criteria:
- Controlled tester package path is accepted if using zip.
- Public release packaging/signing requirements are documented before public release.
- Clean Windows machine test passes before sharing a controlled build.

### Step 5B-16: Human lesson methodology/content sample review

Purpose:
- Manually sample lesson quality, level fit, scenario naturalness, prompt compliance, and multilingual behavior without changing lesson JSON in this planning step.

Acceptance criteria:
- High-priority topic/language combinations are sampled.
- Findings are recorded separately.
- Lesson JSON content remains unchanged in this docs-only planning step.

### Step 5B-17: CMS/Admin operational readiness audit after desktop hardening

Purpose:
- After desktop hardening, audit the minimum Admin/CMS support operations needed before public release.

Acceptance criteria:
- Read-only support/admin needs are identified first.
- Full CMS content management remains deferred until operational requirements are clear.
- Admin/CMS work does not block controlled tester release unless support risk is unacceptable.

### Step 5B-18: Desktop security/privacy release checklist

Purpose:
- Validate token storage decision, diagnostics privacy, microphone/audio disclosure, AI/backend processing expectations, data retention alignment, and no-secrets packaging.

Acceptance criteria:
- Desktop stores no OpenAI API key.
- Desktop calls backend APIs only for AI features.
- Release notes or privacy copy clearly describe microphone, transcription, AI processing, and account data expectations.

### Step 5B-19: Run and record manual desktop release checklist results

Purpose:
- Execute the release checklist on the intended build and record pass/fail/deferred outcomes.

Acceptance criteria:
- Manual checklist results are recorded with build, backend environment, tester machine, and date.
- P0/P1 failures are triaged before any wider release.

### Step 5B-20: Final P0/P1 triage

Purpose:
- Decide whether the desktop can be shared with controlled testers, must remain internal-only, or is ready for a broader MVP release.

Acceptance criteria:
- All P0 items are fixed or explicitly accepted only for the controlled audience.
- P1 items are fixed, documented, or assigned to follow-up with clear owner/order.
- Production billing and CMS/Admin remain deferred unless the triage explicitly moves them into the next active phase.

## Native languages and localization foundation

Step 5B-2 establishes the native/interface/explanation language foundation for the desktop release path. This is not a study-language expansion and must not change lesson JSON content in this planning step.

### Why this is needed

The current desktop learning flow uses language in several different ways: lesson practice language, user-facing UI language, translation target language, hint/explanation language, feedback language, and summary language. Before public release, the app needs clear terminology and stable language identifiers so future localization work does not accidentally change lesson availability or break user settings.

Expanding native/interface/explanation languages also supports a more global release while keeping the backend as the source of truth for settings and keeping AI calls behind backend APIs.

### Terminology

- **Study language**: the language the user practices or learns in lessons.
- **Native language / interface language / explanation language**: the language used for app UI localization, translation target, hints/explanations, feedback/explanation where applicable, and lesson summaries.

These concepts must stay separate. Step 5B-2 expands native/interface/explanation language options only. It does not expand study languages unless a later task explicitly says so.

### Tier 1 native/interface/explanation languages

- English
- Spanish
- French
- German
- Italian
- Portuguese
- Russian
- Ukrainian
- Polish
- Dutch
- Turkish
- Arabic
- Hindi
- Chinese Simplified
- Japanese
- Korean
- Vietnamese
- Indonesian

### Tier 2 native/interface/explanation languages

- Persian
- Urdu
- Bengali
- Tamil
- Telugu
- Marathi
- Gujarati
- Thai
- Swedish
- Norwegian
- Danish
- Czech
- Romanian
- Greek
- Hebrew

### User-facing features that use native/interface/explanation language

- Settings native language options.
- Interface localization later, with English fallback for missing localization.
- Translate button target language where intended.
- Hint explanation language.
- Feedback/explanation language where applicable.
- Lesson summary language.

### Implementation constraints for the later implementation task

- Keep Study language options separate.
- Add stable language IDs/codes.
- Add display names.
- Add static validation so IDs do not break.
- Keep backend as the source of truth for user settings.
- Desktop must still call backend APIs only.
- Do not store OpenAI keys in desktop.
- Do not add direct OpenAI calls from desktop.
- Do not create an EF migration unless a later implementation step explicitly requires a backend schema change.

### Acceptance criteria for Step 5B-2

- Settings shows the expanded native language list.
- Existing study language list remains unchanged.
- User settings save/load the selected native language.
- Translate uses native language as target where intended.
- Hints/explanations can be requested in native language.
- Summary can be generated/displayed in native language.
- English fallback exists.
- Existing English/Russian cases still work.
- No direct OpenAI calls from desktop.
- No OpenAI API key in desktop.
- No EF migration unless a later implementation step explicitly requires backend schema change.
- Static language list tests pass.
- Desktop Debug and Release builds pass.
- Backend build passes.

### Non-goals for Step 5B-2

- Do not expand study languages.
- Do not rewrite lesson JSON content.
- Do not implement full UI localization in this docs-only step.
- Do not add direct desktop-to-OpenAI calls.
- Do not store OpenAI keys in desktop.
- Do not change billing, Paddle, EF entities, or migrations as part of language-list planning.

### Testing checklist for the later implementation task

- [ ] Verify Settings displays all Tier 1 native/interface/explanation languages.
- [ ] Verify Settings displays Tier 2 languages if included in the implementation slice.
- [ ] Verify each language has a stable ID/code and display name.
- [ ] Verify static validation catches duplicate IDs, empty display names, missing fallback, and unsupported saved IDs.
- [ ] Verify existing study languages are unchanged.
- [ ] Verify selecting a native language saves through backend-backed user settings.
- [ ] Verify restarting the desktop reloads the selected native language from user settings.
- [ ] Verify Translate targets the native language where intended.
- [ ] Verify Hint uses the native/explanation language where intended.
- [ ] Verify feedback/explanation uses the native/explanation language where applicable.
- [ ] Verify Summary can be generated/displayed in the native/explanation language.
- [ ] Verify English fallback for missing localization.
- [ ] Verify existing English and Russian native-language cases still work.
- [ ] Verify desktop has no direct OpenAI calls and no OpenAI API key.
- [ ] Verify Desktop Debug build passes.
- [ ] Verify Desktop Release build passes.
- [ ] Verify Backend build passes.

## What not to do now

- No production Paddle rollout now.
- No production billing enablement now.
- No full CMS now.
- No mobile app-store bridge now.
- No full UI localization implementation in this docs-only step.
- No study language expansion unless a later approved task explicitly requests it.
- No lesson JSON rewrite.
- No direct OpenAI calls from desktop.
- No OpenAI API key in desktop.
- No desktop XAML/code-behind/ViewModel changes in this docs-only planning step.
- No backend code changes in this docs-only planning step.
- No Admin UI changes in this docs-only planning step.
- No billing logic changes in this docs-only planning step.
- No Paddle logic changes in this docs-only planning step.
- No EF entity changes or EF migrations in this docs-only planning step.
- No smoke script changes in this docs-only planning step.
- No app behavior changes in this docs-only planning step.
- No real secrets in tracked files.
