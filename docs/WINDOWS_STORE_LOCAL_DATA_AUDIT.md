# Windows Store / MSIX local data audit

Review date: 2026-06-29.

Scope: documentation-only audit before the first Microsoft Store/MSIX prototype. This document does not implement migration logic, does not add MSIX packaging, does not change direct Inno installer behavior, does not change backend runtime code, does not change database schema, does not change deployment scripts, and does not publish or upload anything.

## Executive recommendation

For the first local MSIX prototype, use **Option A: Store build starts isolated from direct local data**.

Rationale:

- Do not share or manually copy sensitive auth/session data by default.
- Do not manually copy access tokens or refresh tokens.
- Require fresh login in the first Store prototype if the package identity isolates data or if DPAPI portability is not proven.
- Preserve backend-owned account, subscription, entitlement, usage, and lesson history through backend sign-in rather than local token migration.
- Keep existing direct Inno tester users unaffected.
- Keep direct `latest.json` installer update behavior out of the Store/MSIX build.
- Use the channel/update behavior plan in [`docs/WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md`](WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md) before the first MSIX prototype so Store local data stays isolated from direct updater cache/installer state.

## Current direct-install local data model

The direct desktop app uses the stable app-data folder name `LanguageVoiceTutor.Desktop` for current roaming local data. The same constants define current filenames for settings, lesson history, auth session, and backend request diagnostics. Legacy folder names are `EnglishVoiceTutor.Desktop` and `Language Voice Tutor` for rename migration compatibility.

Current direct-install files identified in code:

| Data | Current file | Root | Primary owner |
| --- | --- | --- | --- |
| Auth session | `auth-session.json` | `%APPDATA%\LanguageVoiceTutor.Desktop` | `AuthSessionStorageService` |
| Settings | `settings.json` | `%APPDATA%\LanguageVoiceTutor.Desktop` | `UserSettingsService` |
| Lesson history cache | `lesson-history.json` | `%APPDATA%\LanguageVoiceTutor.Desktop` | `LessonHistoryService` |
| Backend request diagnostics | `backend-request-diagnostics.log` | `%APPDATA%\LanguageVoiceTutor.Desktop` | `BackendRequestDiagnosticsService` |
| Direct update installer cache | downloaded installer `.exe` files | `%LOCALAPPDATA%\LanguageVoiceTutor\Updates` | `UpdateDownloadService` |

## Current known local data paths and path-building logic

- `LocalUserDataMigrationService.GetCurrentRoamingFilePath(fileName)` builds `%APPDATA%\LanguageVoiceTutor.Desktop\<fileName>`.
- `LocalUserDataMigrationService.BuildFilePathCandidates(fileName, includeLocalCurrentPath)` starts with the current roaming path, then adds legacy roaming and local paths under `EnglishVoiceTutor.Desktop` and `Language Voice Tutor`, and optionally adds `%LOCALAPPDATA%\LanguageVoiceTutor.Desktop\<fileName>`.
- Settings and lesson history copy the first legacy file to the current roaming path only when the current file is missing.
- Auth session does not use the generic file-copy helper; it attempts to load current and legacy auth-session candidates, decrypts or reads supported legacy formats, then rewrites a valid session through the current auth-session save path.
- Direct update downloads are cached outside the stable roaming app-data folder in `%LOCALAPPDATA%\LanguageVoiceTutor\Updates`.

## Actual classes and files inspected

### Local data, auth, settings, and history

- `Constants/StorageConstants.cs` — current app-data folder, stable folder alias, local data filenames, and legacy folder names.
- `Services/LocalUserDataMigrationService.cs` — current roaming path builder, candidate path builder, and legacy copy helper.
- `Services/Auth/AuthSessionStorageService.cs` — DPAPI-protected auth-session save/load/clear, legacy auth purpose support, legacy session migration, access/refresh expiration checks, and refresh threshold logic.
- `Models/Auth/StoredAuthSession.cs` — stored auth-session shape including access token, refresh token, access expiration, refresh expiration, and cached user.
- `Models/Auth/AuthUserDto.cs` — cached user/account fields in the stored auth session.
- `Services/UserSettingsService.cs` — settings load/save, default settings, settings normalization, and backend URL normalization.
- `Models/UserSettings.cs` — persisted settings fields including interface/native/study language, tutor avatar, speech voice, display name, learning goal, backend base URL, and audio input device.
- `Services/LessonHistoryService.cs` — lesson-history cache load/save, owner-key association, signed-in visibility, legacy ownerless handling, and legacy file copy.
- `Models/LessonHistoryItem.cs` — local lesson-history cache fields, selected level, owner identifiers, summary text, useful phrases, and backend sync IDs.
- `ViewModels/MainViewModel.cs` — settings loaded into runtime, settings save callback, lesson start settings usage, selected level handoff, and lesson history add/sync paths.
- `ViewModels/SettingsViewModel.cs` — Settings UI state, backend settings sync, account/session actions, diagnostics text, update command, and save callback.
- `ViewModels/LessonHistoryViewModel.cs` — local history restore first, backend history overlay when available.

### Backend URL lock and backend-only AI boundary

- `Constants/BackendConstants.cs` — production and developer backend base URLs, default backend base URL resolution, backend endpoints, and backend-configured AI model labels.
- `Services/BackendEndpointBuilder.cs` — DEBUG versus release backend URL normalization, release lock to production backend, and unsafe release override detection.
- `Services/LessonChatBackendService.cs` and backend client services — desktop lesson/audio/translation/hint/summary calls go through backend endpoints and attach auth tokens when available.

### Direct update behavior

- `Services/Updates/UpdateManifestClient.cs` — hard-coded direct manifest URL `https://languagevoicetutor.com/releases/windows/direct/latest.json`, manifest request headers, HTTPS validation, manifest identity validation, installer URI validation, and checksum metadata validation.
- `Services/Updates/DesktopStartupUpdateCheckService.cs` — startup update check delay, newer-version detection, user confirmation before download, active-lesson guard before installer launch, and installer launch prompt.
- `Services/Updates/UpdateDownloadService.cs` — HTTPS installer download, `.exe` filename validation, local update cache path, SHA-256 verification, and delayed detached installer launch.
- `ViewModels/SettingsViewModel.cs` — manual Settings **Check for updates** behavior also uses `UpdateManifestClient` and `UpdateDownloadService`.

### Direct installer and release policy files

- `installer/windows/LanguageVoiceTutor.iss` — current direct Inno installer script; inspected only to confirm this audit does not change it.
- `scripts/package-windows-inno-release.ps1` — direct release package/latest manifest generation and installer naming policy.
- `scripts/validate-windows-direct-release.ps1` — direct release validation for manifest shape, update mode, backend URL, installer naming, and hash consistency.
- `scripts/upload-windows-direct-release.ps1` — direct release upload flow for public direct release files.

## Sensitive data that must never be logged or copied unsafely

The following are sensitive and must not be printed in docs, logs, tests, examples, screenshots, issue text, or command output:

- Auth access tokens.
- Auth refresh tokens.
- Any auth-session file contents, even if DPAPI-protected or base64-looking.
- JWT secrets and bearer tokens.
- OpenAI API keys or provider credentials.
- Paddle keys, webhook secrets, checkout tokens, or private payment/customer identifiers.
- Database connection strings.
- Private user account data such as full auth-session JSON, email-associated owner keys, raw lesson text, transcripts, or private summaries.
- Backend admin tokens or Website CMS admin bearer tokens.

The current auth-session storage uses Windows DPAPI with `DataProtectionScope.CurrentUser` and app-specific entropy for the direct app. That protects at rest for the current Windows user, but it does not make the payload safe to print, copy, attach, or import blindly across package identities.

## Backend source of truth versus local cache

### Backend source of truth

- Account identity and sign-in state after authentication.
- Subscription, entitlement, usage, and limits.
- Lesson sessions and synchronized lesson history available from backend endpoints.
- User settings when authenticated backend settings sync succeeds.
- CMS/Admin runtime content and AI Models CMS runtime settings in persistent backend storage outside release folders.
- Backend-configured OpenAI model/provider settings; the desktop must not call OpenAI directly.

### Local cache or local preference

- Auth-session file: local credential/session cache only; backend remains authoritative and expired refresh tokens should force re-login.
- Settings file: local copy of user preferences and backend base URL value, normalized by build type. Backend settings may override/restore when signed in and reachable.
- Lesson-history file: local recent completed lesson cache with owner keys; backend history should replace or recover signed-in visible history when available.
- Backend diagnostics log: local troubleshooting log and not source of truth.
- Direct update downloaded installer cache: temporary local direct-channel artifact only.

## Data that direct installer updates must preserve

The existing direct Inno update flow should preserve:

- `%APPDATA%\LanguageVoiceTutor.Desktop\settings.json`.
- `%APPDATA%\LanguageVoiceTutor.Desktop\lesson-history.json`.
- `%APPDATA%\LanguageVoiceTutor.Desktop\auth-session.json`, subject to normal expiration/corruption handling.
- Legacy rename migration behavior from `EnglishVoiceTutor.Desktop` and `Language Voice Tutor` when current files are missing.
- Backend release lock behavior for packaged release builds.

This audit does not recommend changing the direct installer, direct manifest, or direct updater.

## Store/MSIX visibility considerations

A future Store/MSIX build may or may not see the current direct app-data paths depending on the final packaging model, package identity, process identity, install location, and any app execution alias/identity behavior. Do not assume that:

- The Store build can read `%APPDATA%\LanguageVoiceTutor.Desktop`.
- DPAPI-protected direct auth payloads can be decrypted under Store identity without testing.
- Both direct and Store builds can safely write the same JSON files concurrently.
- Direct update cache files are meaningful or safe for Store builds.

## Risks of sharing local data between direct and Store builds

- Store and direct builds could race on `settings.json` or `lesson-history.json` if both are installed and used.
- A Store build could inherit a direct backend URL override in DEBUG/prototype scenarios unless release lock and channel behavior are reviewed.
- Direct update metadata/cache could surface in Store build if the direct update service remains enabled.
- Auth-session sharing could unintentionally extend sign-in across package boundaries without explicit user intent.
- DPAPI purpose and Windows user binding may work in some cases but fail in others, causing confusing sign-outs or session deletion.
- A bad Store prototype could damage direct tester state if it writes shared files.

## Risks of isolating local data between direct and Store builds

- Users may appear signed out in the Store prototype.
- Settings such as study language, native language, tutor/avatar, speech voice, display name, learning goal, and audio input device may reset locally.
- Local-only lesson history could appear missing until backend history loads after sign-in.
- Testers may report duplicate setup friction even though backend account/history remains intact.

## Risks of one-time import from direct data into Store build

- Importing `auth-session.json` is risky and should not be done in the first prototype because token copying is sensitive and DPAPI/package identity behavior must be proven first.
- Importing settings can carry stale or development-only backend URL values unless release lock/channel policy is explicit.
- Importing lesson history can duplicate or expose owner-linked records if the user signs into a different account in Store.
- Import failures must be non-destructive to direct data and must not delete direct files.
- The import must be idempotent and auditable without logging private values.

## Recommendation options

### Option A: Store build starts isolated from direct local data

Pros:

- Safest for sensitive auth/session data.
- Avoids corrupting direct tester state.
- Cleanly validates Store package identity and Store-managed update behavior.
- Keeps the first prototype focused on MSIX packaging, launch, sign-in, backend calls, microphone permissions, and update-channel separation.

Cons:

- First Store prototype likely requires fresh login.
- Settings may reset unless backend settings restore after sign-in.
- Local-only history may appear absent until backend history loads.

Risks:

- Tester confusion if not communicated clearly.
- Any locally cached history not synced to backend may not be visible in the Store prototype.

Recommendation: **Preferred for first MSIX prototype**.

### Option B: Store build imports selected safe direct settings once

Pros:

- Reduces setup friction by copying low-risk preferences such as interface language, native language, study language, tutor/avatar, speech voice, learning goal, and audio input device.
- Keeps auth tokens isolated if explicitly excluded.
- Can be designed as a one-time, read-only import from direct path.

Cons:

- Requires implementation, idempotency marker, clear user messaging, and tests.
- Must sanitize/normalize backend URL and avoid importing direct update state.
- Must avoid importing private user text where not necessary.

Risks:

- Incorrect import could copy stale settings or owner-linked metadata.
- Could create hard-to-debug differences between direct and Store builds.

Recommendation: Good follow-up after Option A prototype proves package identity/update behavior.

### Option C: Store build shares the same local data path with direct install

Pros:

- Lowest apparent user friction if it works.
- Settings, local history, and auth session may appear continuous.

Cons:

- Highest coupling between channels.
- Direct and Store builds can conflict if both are installed.
- Store build could accidentally use direct update/latest.json behavior or damage direct state.
- Auth-session sharing across package identities is not proven safe.

Risks:

- Sensitive token/session exposure or invalidation.
- Corruption of direct tester data.
- More complex support and rollback if Store prototype fails.

Recommendation: **Do not use for the first MSIX prototype**.

## Manual test plan: direct install to Store prototype install

1. On a Windows VM/test account, install the current direct Inno tester build.
2. Sign in with a test account; do not capture or print token values.
3. Set interface/native/study language, selected level, tutor/avatar, speech voice, learning goal, and audio input device.
4. Complete a short lesson and confirm backend history appears for the signed-in account.
5. Confirm direct Settings **Check for updates** uses the direct `latest.json` flow and prompts before download/install.
6. Install the Store/MSIX prototype side-by-side if supported by the chosen identity.
7. Confirm Store prototype launch does not call or display direct `latest.json` update behavior.
8. Confirm Store prototype requires fresh login or clearly restores only backend-owned state after login.
9. Confirm backend account, entitlement, usage/limits, and backend lesson history recover after login.
10. Return to the direct install and confirm direct settings, auth/session, history, and update behavior were not modified by the Store prototype.

## Manual test plan: Store prototype uninstall/reinstall

1. Install Store/MSIX prototype on a Windows VM/test account.
2. Sign in and complete a short lesson.
3. Close and reopen the Store prototype; verify expected auth-session behavior for the Store package only.
4. Uninstall the Store prototype using the intended Windows uninstall path.
5. Reinstall the same Store prototype.
6. Verify whether Store app data persisted or was removed according to MSIX behavior and document the observed result.
7. Sign in again if needed; confirm backend-owned account, entitlement, usage/limits, settings sync, and history restore.
8. Confirm the direct Inno app, if installed, remains unaffected.

## Manual test plan: auth session, settings, history/progress, and update behavior

Auth/session:

- Sign in, close, relaunch, and verify session restore without printing tokens.
- Let or simulate access-token expiration and verify refresh-token path works only through backend auth APIs.
- Let or simulate refresh-token expiration and verify the app requires re-login.
- Verify corrupt/unreadable auth-session files are ignored safely without logging file contents.

Settings:

- Change interface language, native language, study language, tutor/avatar, speech voice, learning goal, backend URL in DEBUG only, and audio input device.
- Relaunch and verify normalized values load.
- Verify release builds lock backend URL to production.

History/progress:

- Complete lessons under a signed-in account and verify local history cache and backend history behavior.
- Sign in as a different account and verify owner-key filtering prevents cross-account local history display.
- Verify backend history is preferred when backend history fetch succeeds.

Update behavior:

- Direct build: manual/startup update checks may use `https://languagevoicetutor.com/releases/windows/direct/latest.json`, prompt before download/install, verify SHA-256, and avoid starting an installer during an active lesson.
- Store build: must not check, download, cache, or launch the direct Inno installer from `latest.json`; updates should be Store-managed.

## Existing tests and policy checks found

No dedicated desktop unit test project was found in this repository for the WPF desktop services. Existing coverage relevant to this audit is mostly script/policy/documentation oriented:

- `tools/run_desktop_release_gate.ps1` — desktop release gate aggregator.
- `tools/audit_desktop_backend_boundary.ps1` — checks desktop/backend boundary, including that desktop must not call OpenAI directly.
- `tools/smoke_desktop_backend_routes.ps1` — desktop/backend route smoke coverage.
- `tools/smoke_single_active_lesson_guard.ps1` — backend-enforced active lesson/session behavior.
- `scripts/validate-windows-direct-release.ps1` — direct release `latest.json`, installer filename, update mode, backend URL, and hash validation.
- `scripts/package-windows-inno-release.ps1` — direct installer naming/version and manifest generation policy.
- `backend/EnglishVoiceTutor.Api.Tests/Services/Website/WebsiteContentServiceRenderingTests.cs` — verifies website rendering references the direct Windows `latest.json` path.
- `backend/EnglishVoiceTutor.Api.Tests/*` — backend/admin/website tests; not direct desktop local-data persistence tests.

Gaps before implementation:

- Add desktop tests for auth-session persistence, refresh-token restore, refresh-token expiration, corrupt session handling, and DPAPI behavior on Windows.
- Add desktop tests for local user-data rename migration from legacy paths.
- Add desktop tests for settings persistence and backend release URL lock behavior.
- Add desktop tests for local history owner-key filtering and backend history overlay behavior.
- Add Store-channel policy tests that fail if Store builds reference direct `latest.json` or launch direct Inno installers.

## Open decisions before implementation

- Final Store package identity and whether direct/Store installs can coexist.
- Store channel build flag/constant design and compile-time/runtime boundary for update behavior.
- Whether the Store prototype should use a different app-data folder name from direct install.
- Whether any one-time settings import is allowed after the first isolated prototype.
- Which settings are safe to import, if any.
- Whether local lesson-history cache should ever be imported, or whether backend history is sufficient.
- Auth-session policy for Store: fresh login only versus tested platform-supported continuity.
- Store-specific diagnostics wording that does not expose tokens, secrets, private user data, or direct-channel internals.
- Windows App Certification Kit command/process after a real MSIX exists.

## Recommended follow-up task

Plan and implement a Store channel build flag/update-behavior boundary after this audit is reviewed. That follow-up should make Store builds unable to use the direct `latest.json` installer update flow while leaving the direct Inno release flow untouched. It should include tests/policy checks before any MSIX packaging is added.

## Deployment impact classification

Documentation-only: no deploy needed.

- Backend runtime code changed: no.
- Desktop runtime code changed: no.
- Database schema changed: no.
- Deployment scripts changed: no.
- Installer scripts changed: no.
- Store/MSIX packaging added: no.
- Website CMS/static site published: no.
- Secrets printed or documented: no.
