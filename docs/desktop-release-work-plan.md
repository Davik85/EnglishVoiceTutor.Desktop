# Desktop Release Work Plan


## Parallel Windows installer/update foundation

A minimal Velopack foundation exists for controlled Windows tester installer/update work. It is documented in [`docs/WINDOWS_INSTALLER_UPDATE_FLOW.md`](WINDOWS_INSTALLER_UPDATE_FLOW.md) and built with `scripts/package-windows-velopack-tester-release.ps1`. This is not a replacement for the canonical tester zip yet; the accepted tester handoff remains `artifacts/packages/LanguageVoiceTutor.Desktop-win-x64-self-contained.zip` from `scripts/package-tester-release.ps1` until install/update smoke is accepted.

This foundation does not make the app public-release ready, does not add Microsoft Store/MSIX packaging, does not enable production billing, and does not add production RBAC/Admin readiness. Follow-up work must add update UI and active-lesson-safe update confirmation before updates are offered to testers inside the app.


Review date: 2026-06-06.

## Current conclusion

The desktop product has completed a large release-hardening block and is suitable for continued controlled tester validation, but public release is not declared ready. The tester ZIP package flow, accepted Welcome screen polish, Lesson Chat window auto-sizing, core Lesson Chat/voice/TTS flow, Release Diagnostics gate, protected auth session storage, and backend-enforced single active lesson guard are accepted. External tester handoff is now paused so CMS/Admin content MVP can start before testers review content. Production billing remains deferred.

## Source documents reviewed

- `README.md`
- `docs/CURRENT_STATE.md`
- `docs/NEXT_STEPS.md`
- `docs/LOCAL_RELEASE.md`
- `docs/TESTER_RELEASE.md`
- `docs/desktop-release-smoke-gate.md`
- `docs/desktop-release-readiness-audit.md`
- `docs/desktop-upgrade-paywall-ui-plan.md`
- `docs/subscription-billing-foundation.md`
- `docs/paddle-production-readiness-checklist.md`
- `docs/billing-remaining-operations-plan.md`
- `docs/CMS_ADMIN_PLANNING.md`

## Hard boundaries

- Backend remains the source of truth for account, trial, subscription, Premium/free status, usage, limits, lesson history, active lesson state, payments, entitlements, and AI/TTS/STT calls.
- Desktop must call backend APIs only and must not store or call OpenAI directly with an OpenAI API key.
- `OPENAI_API_KEY` is backend-only, required only for real AI/TTS/STT testing, and must never be committed or sent to testers.
- Language Voice Tutor remains global, cross-platform, and provider-agnostic.
- Do not introduce YooKassa, Russian payment flows, or Russia-only billing assumptions.
- Do not change Paddle, billing, subscription, entitlement, Admin UI, lesson JSON, Study languages, Interface languages, Native/Explanation language catalog, database schema, or backend AI behavior in this documentation/hardening step.

## Updated priority order

### Phase 5B — Desktop release hardening (stable enough to pause tester handoff)

Desktop hardening is accepted enough to pause tester handoff and start CMS/Admin content MVP first. The canonical tester package flow remains the later handoff path after CMS content foundation is ready:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

Default tester artifact:

```text
artifacts\packages\LanguageVoiceTutor.Desktop-win-x64-self-contained.zip
```

Manual `dotnet publish` is only a lower-level developer/troubleshooting path.

### Phase 5D — CMS/Admin content MVP (current focus before tester handoff)

CMS/Admin content MVP now starts before external tester handoff because desktop hardening is stable enough to pause delivery. Development/admin Admin CMS Content now covers content pack overview, topic/scenario/prompt template/tutor behavior profile editing, Step 5D-6e Scenarios editor usability refinement (compact local Jump to navigation, collapsible/visually separated structured sections, helper text, structured fields as the normal path, and Advanced JSON as a technical fallback), validation/preview summary, versions/publish/restore flow with immutable published history, refresh resilience, unsaved-change warnings, required publish summary validation, draft-save publish discoverability, audit filtering, and the controlled local runtime published-snapshot read path. The next recommended CMS implementation step is another CMS/admin improvement, not billing: Validation & Preview/content QA workflow ergonomics. Full production Admin, production RBAC, critical-change approval, and operational support scope remain deferred.

### Phase 5C — Production billing readiness (deferred)

Keep production Paddle rollout after CMS/Admin content MVP and controlled tester-handoff decisions. Production billing must not be treated as enabled until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely outside tracked files and without committing secrets.

## Current priority change: CMS before tester handoff

The desktop hardening block is stable enough to pause external tester handoff and start CMS/Admin content MVP first. The accepted desktop state remains valid, but the next external tester package should wait until the CMS/Admin content editing foundation is ready enough to handle tester-driven content, prompt, scenario, and tutor-behavior fixes without code or lesson JSON changes.

This priority change does not make public release ready. It does not move production billing forward. Production billing, Paddle production operations, payment editing, entitlement editing, broad user management, mobile-specific CMS, and full production Admin remain deferred.

CMS content MVP planning lives in `docs/CMS_ADMIN_PLANNING.md` and `docs/cms-content-mvp-plan.md`. Admin CMS publish UX note: `Save draft` remains draft-only and shows **Go to Publish** after successful saves; changed-content publishing still happens only from **Versions & Publish**, requires a short change summary, and displays a clear local validation error when the summary is missing; backend validation details still display on failure. Old published versions are immutable, and restore creates a new published version rather than mutating history. Learner runtime is unchanged by default, CMS runtime reads only published snapshots when explicitly enabled, and static JSON remains default. The latest local runtime check confirmed the published-snapshot path with `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`, with `Source=CmsPublishedSnapshot`, version 34, expected content counts, validation passed, and no fallback. The clean-machine checklist and package flow below remain required later before actual external tester delivery.

## Completed or accepted Phase 5B items

### Step 5B-1: Settings final acceptance and Diagnostics Release gate — accepted

- Settings is separated into Learning, Account, Audio, Progress, and Diagnostics areas.
- Packaged Release hides Diagnostics by default.
- Diagnostics can appear in Release only when `EVT_DESKTOP_DIAGNOSTICS=1` is set locally before launch.
- Do not commit that environment variable in scripts, settings, shortcuts, or machine-specific docs.
- Diagnostics and copied diagnostics output must mask secrets, tokens, API keys, environment variables, lesson messages, audio paths, and lesson history content.

### Step 5B-2: Native/interface/explanation language foundation — accepted

- Study languages were not expanded and remain English, French, German, Portuguese, Spanish, and Italian.
- Release-ready Interface languages remain `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Native/Explanation languages remain broad.

### Step 5B-3: Interface localization current phase — closed

- Interface localization for the current release-ready list is closed for this release-hardening phase.
- Future Interface languages should be added only 1-2 at a time after full localization QA.

### Step 5B-4: Backend-unavailable/account UX hardening — accepted for resilience

- Backend-unavailable checks are resilience-only.
- The app should not crash, Settings/Account should remain usable, and backend-required actions should fail gracefully with localized messages.
- Full lesson functionality still requires a reachable backend.

### Step 5B-5: Protected auth session storage — implemented

- Desktop still uses `auth-session.json`, but current Windows storage writes a DPAPI-protected Base64 payload using current-user scope.
- The protected payload contains the serialized session only after decrypting as the same Windows user.
- Old plaintext payloads can be migrated by loading once and saving back protected.
- Docs must not describe the current token as raw plaintext storage.

### Step 5B-6: Single active lesson guard — accepted

- Backend enforces one active lesson per account.
- Heartbeat keeps the active lesson fresh.
- Current heartbeat interval is about 30 seconds; current freshness window is 2 minutes.
- Stale active lessons no longer block forever.
- User can end an active lesson on another device and continue.
- Old session becomes `Abandoned`.
- Old device/session cannot continue.
- Old heartbeat and old lesson-bound message creation are rejected with `lesson_session_ended_elsewhere`.
- UI wording must stay neutral and not use fraud language.
- `tools/smoke_single_active_lesson_guard.ps1` passes in the accepted Windows/backend test environment.

### Step 5B-7: Lesson Chat / Voice / TTS acceptance gate — accepted

Accepted manually:

- Normal Lesson Chat works.
- Conversation Mode works.
- TTS works.
- Voice recognition/transcription works and writes text correctly.
- Translation works.
- Hints work.
- Feedback works.
- Final lesson summary appears.

### Step 5B-8: Tester ZIP package acceptance — accepted

- `scripts/package-tester-release.ps1` is the canonical tester distribution flow.
- Expected ZIP: `artifacts\packages\LanguageVoiceTutor.Desktop-win-x64-self-contained.zip`.
- The ZIP was verified on another Windows device after extraction.
- Extracted app starts.
- Diagnostics is hidden by default.
- Backend connection works.
- Account login works.
- Backend lesson history is visible/preserved.
- Core lesson/voice/TTS flow works.
- Active lesson guard and remote active lesson release work.

### Step 5B-9: Welcome screen polish — completed/accepted

- Welcome screen visual polish is accepted for the current desktop hardening phase.
- Hero copy is neutral for a multi-language learning product and does not present the product as English-only.
- The accepted layout uses a large cover image, compact translucent top text overlay, and translucent bottom action overlay for Start lesson and Settings.

### Step 5B-10: Lesson Chat window auto-sizing — completed/accepted

- Entering Lesson Chat auto-expands the main desktop window when it is too small.
- The accepted behavior keeps the app windowed, does not force fullscreen or maximize, does not shrink a larger user-sized window, and keeps the expanded window within the visible working area where possible.
- The accepted result is a comfortable wide layout with visible, balanced avatar and chat columns and uncramped message text.

## Remaining desktop hardening work

Keep this list separate from completed/accepted work:

1. Re-run and record the release gate before each new tester handoff.
2. Re-run active lesson guard smoke when backend/session code changes.
3. Final manual clean-machine checklist pass for the next package candidate.
4. Production backend URL/configuration decision for any broader external test.
5. Installer/signing/update/download plan for public release.
6. Security/privacy release checklist review.
7. Final P0/P1 triage.
8. CMS/Admin content MVP foundation before external tester handoff; Step 5D-6e scenario editor usability refinement, draft-save audit logging, required publish summary validation, audit filtering, immutable published-version restore behavior, and the local runtime published-snapshot read path are complete for development/admin MVP scope. The next refinement is another CMS/admin improvement: Validation & Preview/content QA workflow ergonomics. Full production Admin/RBAC/approval readiness remains later.

## Current validation commands

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1
powershell -ExecutionPolicy Bypass -File .\tools\smoke_single_active_lesson_guard.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

## Public release status

Public release is not declared ready. This work plan supports continued controlled desktop hardening and tester packaging only. External tester handoff is paused until CMS/Admin content MVP foundation is ready enough for practical content changes without code edits; the clean-machine checklist remains later before actual external tester delivery.


## CMS runtime gate before tester handoff

Admin CMS workflow remains safe for tester preparation: `Save draft` does not publish, and saved drafts are not runtime-visible until the existing **Versions & Publish** / **Publish current draft** confirmation flow is used. The editor now makes that path discoverable with a post-save **Go to Publish** action. Static JSON remains the default learner runtime source, and the CMS runtime read path remains disabled by default unless explicitly configured and verified with `tools/smoke_cms_runtime_content_read.ps1`.

External tester handoff remains paused even though the controlled local CMS runtime read path has now been verified. The desktop UI and packaged static lesson/prompt/tutor files remain unchanged for this step. Backend runtime content continues to use static JSON unless `CmsContent:UsePublishedSnapshotForRuntime=true` and `CmsContent:ReadPublishedSnapshotEnabled=true` are explicitly configured. The admin/development diagnostic `/api/admin/dev/cms/runtime-content/status` and `tools/smoke_cms_runtime_content_read.ps1` should continue to prove published snapshot reads, expected counts, valid hash, validation passed, and no fallback on the happy path before tester packaging resumes. This is not production CMS/RBAC readiness.
