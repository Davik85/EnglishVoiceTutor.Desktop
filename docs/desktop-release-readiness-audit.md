# Desktop Release Readiness Audit

Review date: 2026-05-30

Status:
Audit only. No implementation in this step.

Consolidated follow-up plan:
See `docs/desktop-release-work-plan.md` for the Phase 5B desktop release work plan that aligns this audit with `docs/NEXT_STEPS.md` and `docs/CURRENT_STATE.md`.

## Documentation synchronization update (2026-06-01)

This audit remains useful as the Step 5A baseline, but the following release-hardening items are now accepted and supersede older risk wording in this file:

- Settings/Diagnostics Release gate: packaged Release hides Diagnostics by default; Release Diagnostics appears only when `EVT_DESKTOP_DIAGNOSTICS=1` is set locally.
- Protected auth session storage: desktop still uses `auth-session.json`, but current Windows storage writes a DPAPI-protected Base64 payload rather than raw plaintext token JSON.
- Core Lesson Chat / Conversation Mode / TTS / transcription / translation / hints / feedback / summary flow is manually accepted.
- Canonical tester handoff is `scripts/package-tester-release.ps1`, producing `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`; `dotnet publish` is only a lower-level troubleshooting path.
- The tester ZIP was verified on another Windows device after extraction, including app start, Diagnostics hidden by default, backend connection, account login, backend history, accepted lesson flow, active lesson guard, and remote active lesson release.
- Backend-enforced single active lesson protection is heartbeat-based, uses a 2-minute freshness window, supports remote release, marks the old session `Abandoned`, and rejects old heartbeat/message actions with `lesson_session_ended_elsewhere`.
- Prompt/dialogue/scenario/bot-behavior quality polishing is intentionally deferred to CMS/Admin.
- Public release is still not declared ready; production billing remains deferred, and CMS/Admin content MVP work is now underway before external tester handoff but is not production-ready.

## Executive summary

English Voice Tutor Desktop is close enough for focused internal validation, but it is not ready for external MVP users without a short release-hardening pass.

What is close to release-ready:
- The core desktop learning path exists: first launch, level/topic/situation selection, Lesson Chat, text messages, hints, translation, TTS playback, voice recording/transcription, Conversation Mode, lesson finish, summary, and history.
- Backend-backed account/auth, settings sync, lesson session persistence, lesson access checks, free usage limits, and subscription status are implemented.
- The desktop upgrade/paywall path correctly depends on backend access and subscription state rather than local Premium decisions.
- Settings is now organized into learning, account, audio, progress, and diagnostics sections, and diagnostics is already wired to be hideable.
- Lesson content has enough breadth for controlled MVP testing: 26 lesson JSON files across everyday, travel, work/business, job interview, restaurant/cafe, and free conversation scenarios.

What is not ready:
- Release packaging and production configuration are not yet complete. The app still defaults to a localhost backend URL, and there is no signed installer or auto-update/download plan.
- Diagnostics and development-oriented status details must be explicitly verified as hidden or acceptable in Release builds before any public release.
- Backend unavailable, wrong backend URL, expired/invalid token, and 401/403/500 flows need manual acceptance testing and likely message polish.
- Voice and Conversation Mode need device-level testing on clean Windows machines, especially microphone missing/denied cases, transcription failures, audio playback failures, and avatar layout at common window sizes.
- Auth session storage now uses Windows DPAPI-protected local `auth-session.json` payloads; final security/privacy review remains required before broad public release.

Biggest blockers:
- No public-release installer/signing/update plan.
- No production backend URL/configuration plan in the desktop release package.
- Diagnostics/dev UI and copied diagnostics output require final Release-mode acceptance.
- Auth session storage now uses Windows DPAPI-protected local `auth-session.json` payloads; final security/privacy review remains required before broad public release.
- Voice/Conversation Mode reliability is not yet proven across clean tester machines.

Recommended next step:
Run a focused Step 5B desktop release-hardening sequence before returning to production billing or CMS/Admin work. Start with manual acceptance of Settings/diagnostics visibility and backend-unavailable/account UX, then polish Lesson Chat/voice/Conversation Mode, then package and test a Release build on a clean Windows machine.

## Current confirmed baseline

The current confirmed baseline, based on repository docs and release-relevant code review, is:

- EF migrations are current.
- Database update is current.
- No pending model changes are known.
- Lesson content audit passes.
- Desktop Debug build passes.
- Desktop Release build passes.
- Backend build passes.
- Settings no longer crashes after the latest fix.
- Billing sandbox foundation exists.
- Paddle sandbox checkout/webhook/payment loop is validated enough for now.
- Production billing is not the current focus.
- Backend remains the source of truth for account, trial, subscription, Premium/free status, usage, limits, lesson history, payments, and entitlements.
- Desktop must continue to avoid local Premium decisions, real secrets, direct OpenAI calls, and OpenAI API key storage.

## Release readiness score

**Needs focused fixes**

Rationale: the main desktop learning experience and tester ZIP path are accepted for controlled validation, but release readiness still depends on hosted backend configuration, final security/privacy review, clean-machine packaging discipline, installer/signing/update planning, and final P0/P1 triage. If the target is only a tightly controlled internal tester release, this is closer to **Almost ready** after manual acceptance. For external MVP users, it remains **Needs focused fixes**.

## P0 blockers

P0 means the issue blocks app launch, lesson start, account access, data safety, payment access safety, or causes crashes.

- **No confirmed production/hosted backend configuration for desktop release.** The desktop default backend URL is localhost. External users cannot reliably register, log in, check access, start lessons, transcribe audio, or get bot responses without a hosted backend URL and release configuration plan.
- **Production/hosted backend configuration remains unresolved for broader release.** The tester ZIP can work with a reachable backend, but public distribution still needs a production backend URL/configuration plan.
- **Security/privacy final review remains required for broad public release.** Auth session storage is now DPAPI-protected on Windows and Release Diagnostics is hidden by default, but copied diagnostics safety and support procedures still require final release review.

## P1 blockers

P1 means the issue blocks MVP user experience or causes major confusion.

- Backend unavailable/wrong URL/timeout/401/403/500 states need end-to-end manual acceptance for Register, Login, Settings sync, lesson access preflight, Lesson Chat, Hint, Translate, TTS, transcription, checkout launch, and status refresh.
- Sign-in-required and free-limit-used states exist, but the complete new-user path should be manually validated so users understand they must create an account before starting normal lessons.
- Voice recording/transcription must be tested on clean machines with no microphone, changed default microphone, unavailable saved microphone, poor input, and backend transcription failure.
- Conversation Mode needs MVP acceptance for entry/exit, recording UX, auto-send behavior, bot voice auto-play behavior, avatar framing, and small/medium window layout.
- Lesson Chat has many commands and state transitions; text input, send button, hint, translate, play voice, finish, back, and lesson-complete states need a focused UI pass to remove confusing disabled states or unclear status messages.
- Release packaging is limited to a tester zip script. There is no signed installer, signing plan, update plan, or documented public download path.
- CMS/Admin is not ready for public operations. Admin CMS Content now exists for development/admin content editing with refresh resilience and unsaved-change protection, but production RBAC, draft-save audit logging, and approval workflow remain future work. Desktop runtime still uses static JSON by default with CMS reads disabled unless configured and static JSON fallback available.

## P2 polish

P2 means useful improvements that can wait until after first controlled test users if risk is accepted.

- Improve empty/loading states and friendly copy on first launch, Settings, Account, Progress, and History.
- Add clearer user-facing progress around audio loading, TTS generation, translation, and checkout refresh.
- Tighten avatar image framing and Conversation Mode layout at uncommon window sizes.
- Add more content coverage and methodology review beyond the passing JSON audit.
- Add optional automatic checkout status polling after payment; manual Refresh status is acceptable for sandbox/control testing.
- Reduce developer-oriented status indicators in Lesson Chat if they distract normal learners.
- Improve account status wording for trial, Premium, free lesson remaining, enforcement, past due, canceled, and paused states.

## Later / post-MVP

These should not block the first controlled test release:

- Production Paddle rollout and production billing operations automation.
- Full CMS implementation, draft/published workflow, rollback, roles, RBAC, and content versioning.
- Mobile entitlement bridge for Apple App Store / Google Play.
- Automatic update infrastructure.
- Rich analytics dashboards.
- Full multi-avatar expansion beyond Elena and Nelli.
- Larger lesson catalog and advanced placement testing.
- Fully automated UI tests for all desktop states.

## Area-by-area audit

### 1. First launch and app shell

**Current status**
- The app has a Welcome screen with a clear primary path into level selection and a Settings entry point.
- The learning path is understandable: Welcome -> Level -> Topic -> Subtopic/Situation -> Lesson Chat.
- Home exposes topic selection, History, Settings, and Back.
- Access/paywall panels are handled at the app-shell level when lesson start is blocked.

**Risks**
- A new user may not know that account creation is required until they try to start a lesson.
- Development-style status dots and diagnostic/status language may be visible in normal flows.
- Empty/error/loading states are present in several places but have not been accepted as learner-friendly.

**Release recommendation**
- Keep the current navigation model for MVP, but manually test the first-run path as a brand-new user.
- Make sure any development-only status surfaces are hidden or acceptable in Release.

**Suggested next task**
- Step 5B-1: First-launch and app-shell manual acceptance pass.

### 2. Settings

**Current status**
- Settings is organized into separate Learning, Account, Audio, Progress, and Diagnostics sections.
- Diagnostics has an `IsDiagnosticsTabVisible` gate in the view, so it is designed to be hideable.
- Account, learning preferences, audio input, progress/statistics, backend URL, and diagnostics are separated enough for MVP.
- Recent fixes indicate Settings no longer crashes.

**Risks**
- Diagnostics currently contains backend URL, settings path, lesson-history path, database/config status, and copyable technical output. This is useful for testers but too technical for normal users.
- Backend URL editing belongs in diagnostics/tester builds, but it may confuse normal public users if visible.
- Account status copy is functional but may need friendly wording for nontechnical users.

**Release recommendation**
- Settings is good enough for controlled testers if Diagnostics remains visible by plan.
- For public MVP, hide Diagnostics and backend URL editing by default, or make it available only through an explicit support/debug path.

**Suggested next task**
- Step 5B-2: Settings final visual/manual acceptance and Diagnostics Release visibility decision.

### 3. Account/auth

**Current status**
- Registration, login, logout, restore session, authenticated settings, and account status display exist in the desktop Settings flow.
- Signed-out users are blocked from starting normal lessons.
- Registration grants a backend-managed trial; login does not create or extend trial.
- Account subscription status displays plan, Premium state, trial, free lesson remaining/used, enforcement, source, and checked time.

**Risks**
- Backend unavailable and wrong backend URL cases are mapped to friendly messages in some paths, but all auth paths need manual testing.
- Token/session storage uses a local `auth-session.json` file whose current Windows payload is DPAPI-protected for the current user.
- Expired token/session behavior appears to fall back to signed-out/development settings in some flows; this could confuse users unless messaging is clear.
- Login/register errors may be too generic for common cases such as duplicate email, weak password, invalid email, wrong password, backend down, or server error.

**Release recommendation**
- Account/auth is acceptable for internal controlled testing.
- Public release should not proceed until protected token storage, expired/invalid token UX, and support diagnostics procedures are deliberately accepted in final security/privacy review.

**Suggested next task**
- Step 5B-3: Account/auth UX hardening audit and token-storage decision.

### 4. Backend connection handling

**Current status**
- The desktop centralizes backend URLs and adds ngrok warning-skip headers for backend calls.
- Lesson Chat maps timeout, validation, unexpected response, backend unavailable, and server error cases to user-facing status messages.
- Lesson start preflight blocks lesson start if the backend decision is unavailable.
- Diagnostics can show backend, database, and AI configuration status.

**Risks**
- The default backend URL is `http://localhost:5000`, which is appropriate for development but not for external users.
- Wrong URL or backend-down states may appear at different points: Settings, auth, lesson start, chat, hint, translate, TTS, transcription, checkout, and refresh.
- Technical details are logged to Debug output and diagnostics; normal Release UX must avoid raw exception text.
- 401/403/500 handling should be accepted in each client path, not assumed from one mapping function.

**Release recommendation**
- Add a manual backend-unavailable test matrix before MVP.
- Decide whether public builds hardcode a production backend URL or use a packaged configuration file that contains no secrets.

**Suggested next task**
- Step 5B-4: Backend unavailable/wrong URL/401/403/500 manual test and copy polish.

### 5. Lesson selection flow

**Current status**
- Level selection supports A1, A2, B1, and B2.
- Topic selection includes Everyday English, Travel, Work and Business, Job Interview, Restaurant and Cafe, and Free Conversation.
- Subtopic/situation selection is explicit, with five situations per main topic and one open conversation situation.
- Lesson start guard checks backend lesson access before entering Lesson Chat.
- Signed-out state, free lesson available/used, trial, Premium, past due, canceled/paused, unknown/error, and checkout unavailable states are mapped to display models.

**Risks**
- If backend access is unavailable, users are blocked with a generic access-check message; this is safe but may be frustrating if the cause is wrong Backend URL.
- The free-limit-used message says upgrade options are available soon, but the current app also has an Upgrade action when checkout is configured; wording should be checked for consistency.
- Free Conversation may need a release note explaining it is still bounded by safety and learning rules.

**Release recommendation**
- Keep the backend-driven guard. Do not add local Premium/free decisions.
- Manually validate all access states before release.

**Suggested next task**
- Step 5B-5: Lesson selection/access-state acceptance test.

### 6. Lesson Chat

**Current status**
- Lesson Chat has initial lesson context, chat messages, text input, send, Enter-to-send behavior, Hint, Translate, Play voice, Finish lesson, Back, selected feedback, and status indicators.
- Lesson Chat uses backend services for AI, translation, TTS, transcription, lesson session, and lesson messages.
- Backend errors are generally converted to friendly status messages rather than raw exceptions.
- Message readability appears designed with distinct bot/user cards and auto-scroll behavior.

**Risks**
- There are many command states: sending, recording, bot voice playing, lesson completed awaiting finish, lesson limit reached, realtime state, setup/context selection, and finished. Disabled controls can confuse users if no explanation is shown.
- Developer-oriented status dots/tooltips may be too technical for normal users.
- Initial bot message, end-of-lesson behavior, and should-end-lesson logic need manual validation across text and voice turns.
- Back behavior finishes backend session and navigates away; users may need clearer confirmation if progress could be incomplete.

**Release recommendation**
- Do a focused Lesson Chat release polish pass after backend/account UX.
- Keep all AI calls through backend services.

**Suggested next task**
- Step 5B-6: Lesson Chat command-state and messaging polish.

### 7. Voice flow

**Current status**
- Settings supports microphone selection, refresh microphones, and microphone test.
- Lesson Chat has start/stop recording, transcription, and auto-send voice behavior where configured.
- Saved microphone fallback/unavailable state is represented in Settings.
- Audio recording temporary files are cleaned up after tests and use.

**Risks**
- Microphone unavailable, permission denied, device removed after selection, and silent/poor audio cases need clean-machine testing.
- Transcription failure messaging should be checked for clarity and retry guidance.
- Auto-send voice can surprise users if transcript quality is poor; controlled testers should verify it is acceptable.
- The app depends on backend transcription; backend unavailable must produce a friendly voice-specific failure.

**Release recommendation**
- Voice is a core differentiator and should be P1-tested on multiple Windows devices before external users.

**Suggested next task**
- Step 5B-7: Voice recording/transcription reliability test pass.

### 8. Bot voice / TTS

**Current status**
- Bot TTS is backend-driven through audio speech endpoints.
- Lesson Chat supports bot voice auto-play rules and manual Play voice.
- Audio generation and playback include cancellation, temp-file cleanup, and avatar speaking-state integration.
- Conversation Mode can use the stable TTS pipeline rather than the future realtime path.

**Risks**
- Audio loading state may not be obvious enough to users.
- Audio failure should be clearly recoverable with text still usable.
- TTS latency and cancellation during Back/Finish/Conversation Mode transitions need manual stress testing.
- Avatar speaking animation must not get stuck after failed/canceled playback.

**Release recommendation**
- Keep TTS backend-only and test it under slow backend/network conditions.

**Suggested next task**
- Step 5B-8: Bot voice/TTS loading, failure, and avatar-state acceptance.

### 9. Conversation Mode

**Current status**
- Lesson Chat has an entry point into Conversation Mode.
- Conversation Mode presents the selected avatar, latest user/bot text overlays, Hint, record button, and return-to-chat action.
- The current MVP path can use stable TTS; realtime code remains present as future capability but is not the default MVP path.
- Recording and bot voice states are integrated with Conversation Mode state.

**Risks**
- Large avatar layout, overlay readability, and button reachability need testing at common window sizes and DPI settings.
- Users need a clear understanding of whether the record button is click-to-start/stop or hold-to-talk.
- Auto-send voice and auto-play bot voice can create confusing loops if microphone picks up speaker audio; headphones guidance may be needed.
- Return/back flow must reliably stop recording/audio and preserve the lesson state.

**Release recommendation**
- Treat Conversation Mode as MVP-conditional: include it if the acceptance checklist passes; otherwise hide or label it as beta for controlled testers.

**Suggested next task**
- Step 5B-9: Conversation Mode MVP acceptance or beta/hide decision.

### 10. Avatar behavior

**Current status**
- Two tutor avatars exist: Elena and Nelli.
- Settings supports avatar selection and profile display.
- Tutor profile JSON constrains identity, style, speaking rules, and personal details.
- Lesson Chat and Conversation Mode use avatar image/animation assets and speaking state.

**Risks**
- Avatar image/framing may vary between chat and Conversation Mode layouts.
- The profile details are useful, but too much personality may distract from lesson goals if prompt compliance is not manually reviewed.
- Only two avatars are available; this is acceptable for MVP but should not be overmarketed.

**Release recommendation**
- Elena and Nelli are enough for MVP if framing and prompt/personality behavior pass manual review.
- More avatars can wait.

**Suggested next task**
- Step 5B-10: Avatar framing/profile/prompt acceptance pass.

### 11. Lesson ending and summary

**Current status**
- Lesson Chat has Finish lesson and automatic lesson-complete-awaiting-finish behavior after enough learner turns.
- Lesson summary uses eligible conversation messages and can save backend summary data for authenticated sessions.
- Backend lesson session finish is attempted on Finish and Back.
- Progress/statistics and History are available.

**Risks**
- Exiting early through Back may finish the backend lesson session without a user-facing summary; this should be accepted or clarified.
- Summary save failure logs Debug output but should not disrupt the user; this is acceptable if History behavior is clear.
- shouldEndLesson behavior needs manual validation for typed turns, voice turns, invalid transcript retries, and Conversation Mode turns.

**Release recommendation**
- Keep current behavior for controlled MVP after manual testing. Consider adding clearer copy later for early exit and completed-awaiting-finish states.

**Suggested next task**
- Step 5B-11: Lesson completion, early exit, summary, and progress manual test.

### 12. Free limits / paywall / upgrade

**Current status**
- Backend consumes a free lesson after a lesson session starts and at least 3 valid user messages are sent.
- Backend can block a new lesson after the daily free lesson is used when enforcement is enabled.
- Desktop preflight checks backend lesson access before navigation.
- Free-limit-used state can show an Upgrade action.
- Checkout is launched through the backend checkout-session endpoint.
- Refresh status manually asks backend access/subscription endpoints after checkout.
- Premium active state is shown only after backend state reports Premium.

**Risks**
- Subscription enforcement default remains development-safe and may need explicit production configuration.
- Manual Refresh status is acceptable but may confuse users if webhook delivery is delayed.
- Checkout unavailable and checkout failed paths should be tested without changing billing logic.
- Wording should avoid promising immediate access until backend reports Premium.

**Release recommendation**
- Do not rework billing in this audit. Keep production billing deferred.
- Validate sandbox/free-limit UX as part of desktop readiness, but keep all access decisions backend-driven.

**Suggested next task**
- Step 5B-12: Free-limit/paywall desktop UX acceptance without billing logic changes.

### 13. Diagnostics and development-only UI

**Current status**
- Diagnostics is a Settings tab that can be hidden through view-level visibility.
- Diagnostics can refresh backend/database/AI settings status and copy a diagnostic report.
- Debug output contains detailed developer traces for lesson state, backend failures, usage summaries, TTS, realtime state, and command-state diagnostics.
- Development admin and diagnostics tooling exist in the repo.

**Risks**
- Diagnostics must not appear to normal Release users unless deliberately enabled for support/testing.
- Copied diagnostics must not include API keys, environment variables, raw lesson messages, raw audio file paths, lesson history content, payment secrets, or tokens.
- Backend URL editing and config status can expose implementation details.

**Release recommendation**
- P0 for public release: confirm Diagnostics hidden/default-off in Release and verify copied report safety.
- Debug-only developer traces can remain if they do not surface in user UI or package logs.

**Suggested next task**
- Step 5B-13: Release diagnostics/config cleanup and copied-output safety check.

### 14. Release build behavior

**Current status**
- Desktop Debug and Release builds are documented as passing.
- The project targets `net10.0-windows` and is a WPF `WinExe`.
- The desktop excludes backend source from the desktop project and packages Content files.
- The default backend URL constant is localhost.
- A tester packaging script publishes a win-x64 Release build.

**Risks**
- Debug vs Release differences for Diagnostics visibility are not fully documented in code-level release acceptance.
- Localhost default is not release-ready for external users.
- Production backend URL readiness is unresolved.
- Config safety requires no real secrets in desktop files, docs, package output, or appsettings.

**Release recommendation**
- Define explicit Release build configuration expectations before packaging external builds.
- Keep secrets backend-only and outside tracked files.

**Suggested next task**
- Step 5B-14: Release build config checklist and production backend URL decision.

### 15. Installer / distribution readiness

**Current status**
- There is a tester package script that publishes a win-x64 self-contained zip by default and can publish framework-dependent packages.
- The script checks that the desktop exe exists and rejects obvious forbidden files such as settings/history/API-key-like files in publish output.
- There is no installer project found in the audited repo structure.

**Risks**
- No signed installer.
- No code-signing plan.
- No SmartScreen/reputation plan.
- No clean Windows machine test plan beyond tester docs.
- No public download/update plan.
- No rollback or versioning plan for desktop releases.

**Release recommendation**
- A zip is acceptable only for a controlled tester release.
- Public MVP should have a signed installer or a clearly accepted distribution alternative.

**Suggested next task**
- Step 5B-15: Installer/signing/clean-machine release package checklist.

### 16. Content readiness

**Current status**
- Lesson JSON audit passes.
- The repo contains 26 lesson JSON files.
- Coverage includes Everyday English, Travel, Work and Business, Job Interview, Restaurant and Cafe, and Free Conversation.
- Supported study languages are English, French, German, Portuguese, Spanish, and Italian.
- Prompts and policy tests exist for lesson behavior, multilingual behavior, tutor profile behavior, and lesson turn policy.

**Risks**
- Passing JSON validation does not guarantee teaching quality, scenario naturalness, level accuracy, or full multilingual quality.
- Free Conversation needs careful safety and topic-boundary behavior review.
- Lesson runtime remains static JSON by default. Admin CMS Content can now edit draft topics, scenarios, full scenario JSON, prompt templates, and tutor behavior profiles for development/admin use, but tester handoff remains paused until the CMS/Admin content MVP is ready enough for practical content changes without code edits.

**Release recommendation**
- Content is good enough for controlled MVP if a human samples each topic/language combination most likely to be used.
- Deeper methodology review and CMS-based content operations can wait.

**Suggested next task**
- Step 5B-16: Human lesson methodology/content sample review.

### 17. CMS/Admin dependency

**Current status**
- Local Development CMS/admin support foundation and Admin CMS Content workspace exist, including content pack overview, topic/scenario/full scenario JSON/prompt/tutor editing, validation/preview summary, versions/publish/restore, refresh resilience, selected user/entity restore, and unsaved-change warnings. CMS/Admin is still not mature enough for release because production RBAC, draft-save audit logging, and critical-change approval are not implemented.
- Desktop does not require a full CMS to run current JSON-based lesson content.
- Backend remains the operational source of truth for accounts, usage, payments, entitlements, and lesson history.

**Risks**
- Public release without mature Admin tooling creates support risk: account lookup, entitlement correction, refund/chargeback workflows, content hotfixes, and audit trail.
- Manual/JSON content fixes are acceptable for controlled MVP but slow for public operations.

**Release recommendation**
- Desktop controlled MVP can proceed without CMS if support volume is intentionally limited.
- Before public release, implement CMS draft-save audit logging, later add critical-change approval after production roles exist, and run a separate CMS/Admin v1 audit focused on support/content operations.

**Suggested next task**
- Step 5B-17: CMS/Admin operational readiness audit after CMS draft-save audit logging and production-role decisions.

### 18. Security/privacy

**Current status**
- Desktop should not store OpenAI keys and should not call OpenAI directly; backend APIs are used for chat, hints, translation, transcription, and TTS.
- Payment secrets and provider API keys belong in backend configuration, not desktop code.
- The tester package script rejects obvious settings/history/API-key-like files in publish output.
- Diagnostics docs emphasize not sending API keys or private local files.

**Risks**
- Auth session storage uses Windows DPAPI-protected local `auth-session.json` payloads; final broad-release security/privacy review is still required.
- Diagnostics shows local paths and backend/config status; copied output must be reviewed for privacy.
- Debug logs may contain lesson metadata and usage summaries. Confirm no raw secrets, tokens, raw audio, payment secrets, or sensitive lesson content are persisted in normal Release scenarios.
- Microphone/audio privacy needs clear user-facing expectations: recording happens only when the user starts it; audio/transcription is sent to backend for processing.
- Backend and Admin logs may contain operational data; desktop release should align with the data retention policy.

**Release recommendation**
- P0 for public release: finish security/privacy review for protected auth session storage, Release Diagnostics support procedures, and copied diagnostics output safety.
- Add clear privacy copy/manual release notes for microphone, transcription, AI processing, and account data.

**Suggested next task**
- Step 5B-18: Desktop security/privacy release checklist.

### 19. Smoke/manual test coverage

**Current status**
- Existing scripts cover lesson content audit, UI/policy regressions, realtime/conversation policy, multilingual prompt/transcription policy, tutor profile policy, usage/cost policy, admin foundation, and Paddle/billing smoke scenarios.
- Manual tester docs cover package creation, backend URL setup, diagnostics, microphone testing, Lesson Chat, Hint, Translate, voice recording, Play voice, Conversation Mode, Finish, Summary, History, and restart persistence.

**Risks**
- Scripts are mostly policy/smoke coverage and do not replace manual WPF UI acceptance.
- Clean-machine Windows testing, installer/signing, backend-down UX, wrong backend URL, token expiry, and audio device edge cases are not fully automated.
- Production-like hosted backend behavior is not covered by local scripts.

**Release recommendation**
- Keep scripts unchanged for this audit.
- Add a human desktop release checklist run before any controlled MVP package is shared.

**Suggested next task**
- Step 5B-19: Run and record manual desktop release checklist results.

### 20. Final release blockers

**Current status**
- The core product is implemented, but release readiness has unresolved P0/P1 items.

**Risks**
- The app may work well on the developer machine but fail or confuse users on clean machines because of backend URL, account state, diagnostics, microphone, or packaging issues.
- Public users expect installer/signing/update and privacy/security polish that the current repo does not yet fully show.

**Release recommendation**
- Do not treat the desktop as public-release-ready yet.
- Proceed with a controlled internal/tester release only after P0 items are either fixed or explicitly accepted for that controlled audience.

**Suggested next task**
- Step 5B-20: Final P0/P1 triage after Step 5B hardening tasks.

## Suggested implementation order

1. **Settings final visual/manual acceptance and Diagnostics Release gate**
   - Confirm Settings opens reliably.
   - Confirm Learning, Account, Audio, Progress, and Diagnostics separation.
   - Diagnostics visibility is decided: packaged Release hides it by default and local `EVT_DESKTOP_DIAGNOSTICS=1` enables it for support/testing.
2. **Native languages and localization foundation**
   - Expand native/interface/explanation language options as planned in `docs/desktop-release-work-plan.md`.
   - Keep Study language options separate and unchanged unless a later approved task explicitly expands them.
   - Keep translation targets, hints/explanations, feedback/explanation where applicable, and summaries aligned with backend-backed user settings.
   - Keep desktop AI features backend-only with no OpenAI API key in desktop.
3. **Backend unavailable/account UX**
   - Test backend stopped, wrong URL, timeout, 401, 403, and 500 across auth, settings, lesson access, chat, TTS, transcription, checkout, and refresh.
4. **Lesson Chat release polish**
   - Review initial bot message, command states, disabled controls, status messages, Hint, Translate, Play voice, Finish, and Back.
5. **Voice/recording reliability**
   - Test microphone selection, unavailable device, permission denial, start/stop, transcription failure, and auto-send voice.
6. **Conversation Mode MVP acceptance**
   - Test avatar layout, record UX, auto-send, auto-play, Hint, return/back, and small/medium window sizes.
7. **Release diagnostics/config cleanup**
   - Hide/default-off Diagnostics for normal Release users.
   - Verify copied diagnostics output safety.
   - Decide production backend URL/config path.
8. **Installer/release package checklist**
   - Decide zip vs installer, signing, clean-machine test, production backend URL, versioning, download, update, and rollback.
9. **CMS/Admin v1 audit**
   - After desktop readiness, separately audit only the minimum Admin/CMS operations needed for public support.

## Manual test checklist for desktop release

Use this checklist before sharing a controlled desktop build:

- [ ] Start backend with expected local/test configuration.
- [ ] Confirm backend health endpoint responds.
- [ ] Start desktop Debug build.
- [ ] Start desktop Release build.
- [ ] Confirm first launch is understandable.
- [ ] Open Settings.
- [ ] Set or confirm Backend URL.
- [ ] Register a new account.
- [ ] Confirm registration grants expected backend trial/account status.
- [ ] Logout.
- [ ] Login with the registered account.
- [ ] Confirm account status displays plan, Premium/trial/free lesson state, source, and checked time.
- [ ] Change learning settings: display name, learning goal, study language, native language, interface language if applicable, and tutor avatar.
- [ ] Change audio settings: select microphone or System default.
- [ ] Test microphone from Settings.
- [ ] Save settings.
- [ ] Restart desktop and confirm settings persist or sync correctly.
- [ ] Select level.
- [ ] Select topic.
- [ ] Select subtopic/situation.
- [ ] Start free lesson.
- [ ] Confirm initial bot message appears and is readable.
- [ ] Send one typed message.
- [ ] Use Hint.
- [ ] Use Translate.
- [ ] Use Play voice.
- [ ] Send 3+ valid user messages.
- [ ] Confirm free lesson is consumed in backend/subscription status.
- [ ] Finish lesson.
- [ ] Confirm Summary appears.
- [ ] Confirm saved progress/statistics/history.
- [ ] Try starting another lesson after the free lesson is used.
- [ ] Confirm blocked/free-used state appears when enforcement is enabled.
- [ ] Confirm Upgrade panel appears when appropriate.
- [ ] Launch sandbox checkout only in sandbox/test configuration.
- [ ] Return to desktop and use Refresh status.
- [ ] Confirm Premium active state appears only after backend reports Premium.
- [ ] Logout and verify lesson start is blocked with sign-in-required state.
- [ ] Stop backend.
- [ ] Try Register/Login/Settings sync/Lesson start and confirm backend unavailable messages are clear.
- [ ] Set a wrong Backend URL and confirm errors are user-friendly.
- [ ] Restore correct Backend URL.
- [ ] Test microphone unavailable scenario if possible.
- [ ] Test voice record/transcription.
- [ ] Confirm failed transcription has clear retry guidance.
- [ ] Enter Conversation Mode.
- [ ] Record in Conversation Mode.
- [ ] Confirm auto-send voice behavior.
- [ ] Confirm bot voice auto-play behavior.
- [ ] Return from Conversation Mode to Chat.
- [ ] Finish a lesson after using Conversation Mode.
- [x] Confirm tester ZIP launches after extraction on another Windows device.
- [x] Confirm packaged Release does not show Diagnostics unless deliberately enabled.
- [ ] Confirm copied diagnostics output contains no secrets, tokens, raw audio paths, payment secrets, OpenAI API keys, environment variables, or lesson-history content.

## Recommended next Codex tasks

Use `docs/desktop-release-work-plan.md` as the controlling consolidated plan for Phase 5B. Recommended next tasks are:

1. **Step 5B-1: Settings final acceptance and Diagnostics Release gate**
   - Confirm Settings stability and Release diagnostics visibility/output safety.
2. **Step 5B-2: Native languages and localization foundation**
   - Expand native/interface/explanation language planning and later implementation scope.
   - Keep Study language options separate.
   - Keep backend-backed settings as source of truth.
   - Keep desktop AI features backend-only and do not store OpenAI keys in desktop.
3. **Step 5B-3: Backend unavailable and Account UX hardening**
   - Test stopped backend, wrong URL, expired session, invalid credentials, 401/403/500.
4. **Step 5B-4: Auth session storage production decision**
   - Implemented for Windows with DPAPI-protected `auth-session.json` payloads; keep final security/privacy review before broad public release.
5. **Step 5B-5: Lesson selection and access-state QA**
   - Validate signed-out, free available, free used, trial, Premium, past due, canceled/paused, checkout unavailable, and unknown/error states.
6. **Step 5B-6: Lesson Chat MVP polish**
   - Review command states, status messages, initial bot text, Hint, Translate, Play voice, Finish, Back, and completed-awaiting-finish UX.
7. **Step 5B-7: Voice recording/transcription reliability pass**
   - Test microphone selection, missing devices, permission failures, start/stop recording, transcription failure, and auto-send voice.
8. **Step 5B-8: Bot voice/TTS loading, failure, and avatar-state acceptance**
   - Validate TTS loading/failure/retry states and avatar-state behavior.
9. **Step 5B-9: Conversation Mode MVP acceptance or beta/hide decision**
   - Validate avatar layout, record UX, auto-send, auto-play, return/back, and layout on common window sizes.
10. **Step 5B-10: Avatar framing/profile/prompt acceptance pass**
    - Defer prompt/dialogue/scenario/bot-behavior quality polishing to CMS/Admin; keep only blocking avatar/framing defects in desktop hardening. Do not claim production CMS readiness until RBAC, draft-save audit logging, and approval workflow are addressed.
11. **Step 5B-11: Lesson completion, early exit, summary, and progress manual test**
    - Validate Finish, Back, summary, History, statistics, and progress behavior.
12. **Step 5B-12: Free-limit/paywall desktop UX acceptance without billing logic changes**
    - Validate free-limit/paywall UX while preserving backend-driven access decisions.
13. **Step 5B-13: Release diagnostics/config cleanup and copied-output safety check**
    - Verify Release diagnostics visibility and copied-output safety.
14. **Step 5B-14: Release build config checklist and production backend URL decision**
    - Decide backend URL/config expectations and verify no secrets in desktop release output.
15. **Step 5B-15: Installer/signing/clean-machine release package checklist**
    - Decide zip vs installer, signing, clean-machine test, versioning, download, update, and rollback.
16. **Step 5B-16: Human lesson methodology/content sample review**
    - Sample lesson quality without changing lesson JSON in this audit.
17. **Step 5B-17: CMS/Admin operational readiness audit after CMS draft-save audit logging and production-role decisions**
    - Audit minimum support/admin operations after desktop readiness.
18. **Step 5B-18: Desktop security/privacy release checklist**
    - Validate privacy, token, diagnostics, microphone/audio, AI/backend processing, and no-secrets expectations.
19. **Step 5B-19: Run and record manual desktop release checklist results**
    - Execute and record release checklist results for the intended package/environment.
20. **Step 5B-20: Final P0/P1 triage**
    - Decide controlled tester, internal-only, or broader release readiness.

## Explicit non-goals for this audit

- No code changes.
- No desktop XAML/code-behind/ViewModel changes.
- No backend changes.
- No Admin UI changes.
- No CMS implementation.
- No billing changes.
- No Paddle production rollout.
- No Paddle logic changes.
- No EF entity changes.
- No EF migration.
- No smoke script changes.
- No lesson content changes.
- No app behavior changes.
- No real secrets.
