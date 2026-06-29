# Windows Store / MSIX release plan

Review date: 2026-06-29.

Scope: documentation-only planning for a future Microsoft Store distribution channel. This document does not implement MSIX packaging, does not create a Store submission, does not change the existing Inno Setup direct-download flow, does not change backend deployment, does not run migrations, does not publish Website CMS/static site content, and does not change billing/payment code.

## Official Microsoft references

Use only current Microsoft Learn guidance when the MSIX prototype begins:

- Win32 app distribution through Microsoft Store: <https://learn.microsoft.com/en-us/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store>
- Choosing a Windows app distribution path: <https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path>
- MSIX package requirements: <https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements>
- MSIX app certification process: <https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-certification-process>
- Microsoft Store policies: <https://learn.microsoft.com/en-us/windows/apps/publish/store-policies>
- Microsoft Store listing info for MSIX apps: <https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/add-and-edit-store-listing-info>
- Microsoft Store screenshots/images for MSIX apps: <https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images>
- Windows App Certification Kit: <https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/windows-app-certification-kit>

## Current state: direct Windows release channel

The current Windows desktop release channel is the direct-download controlled tester channel:

- Packaging is Inno Setup based.
- The canonical package script is `scripts/package-windows-inno-release.ps1`.
- The Inno script is `installer/windows/LanguageVoiceTutor.iss`.
- Release validation is performed by `scripts/validate-windows-direct-release.ps1`.
- Upload is performed by `scripts/upload-windows-direct-release.ps1`.
- The public direct manifest is `https://languagevoicetutor.com/releases/windows/direct/latest.json` and the repository copy is `site/public/releases/windows/direct/latest.json`.
- The current direct update mode is `manual-confirmation`.
- Generated release outputs under `artifacts/` are not source of truth and must not be committed.

This channel remains current and working. It must not be removed, renamed, repurposed, or changed as part of Microsoft Store preparation.

## Store strategy decisions

1. Microsoft Store distribution should use **MSIX** as the preferred first path.
2. The existing direct Inno installer channel remains a parallel release channel.
3. Store builds must not use the direct `latest.json` auto/manual installer update flow. The first channel/update runtime guard is implemented with `DesktopDistributionChannel=Direct|Store`; details are documented in [`docs/WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md`](WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md).
4. Store build updates should be managed by Microsoft Store.
5. Paddle/web checkout is the planned payment path for the PC non-game Store build, pending final Partner Center disclosure and policy review.
6. Microsoft Store payment/IAP integration is **not** being implemented in this step.
7. Backend remains the source of truth for account, subscription, entitlement, usage, and limits.
8. Desktop must continue to call backend APIs only and must not call OpenAI directly.
9. Billing architecture must remain provider-agnostic so Apple/Google mobile subscriptions can be bridged later.

## Why EXE/MSI Store submission is not the preferred first path

Microsoft Store can support Win32 EXE/MSI-style distribution paths, but that path would preserve more publisher-owned installer/update responsibilities and would overlap heavily with the existing direct Inno release channel. For this project, the first Store prototype should test the cleanest Store-native update and identity path:

- MSIX provides a Store-managed package identity and update channel.
- Store-hosted MSIX avoids reusing the direct `latest.json` update mechanism.
- MSIX better separates Store install/update behavior from the current direct tester installer behavior.
- EXE/MSI Store submission risks duplicating the current Inno channel without answering MSIX identity, app-data, and Store-update questions.

## Current script/documentation audit

### Direct Windows packaging and release scripts

- `scripts/package-windows-inno-release.ps1` builds the direct Windows Inno package and direct-release metadata.
- `installer/windows/LanguageVoiceTutor.iss` is the Inno Setup script used by the direct channel.
- `scripts/validate-windows-direct-release.ps1` validates direct release artifacts, manifests, update mode, backend URL, and installer hash consistency.
- `scripts/upload-windows-direct-release.ps1` uploads direct release artifacts to the direct website release folder.
- `scripts/package-tester-release.ps1` exists as an older/emergency tester package helper and is not the canonical direct installer channel.
- `site/public/releases/windows/direct/latest.json` is the checked-in public direct manifest snapshot; live truth is the HTTPS manifest.

### Backend release, database, and operations scripts

- `scripts/package-backend-linux-release.ps1` packages backend Linux releases and explicitly does not run migrations.
- `scripts/upload-backend-linux-release.ps1` uploads/deploys backend releases and remains separate from Windows installer upload.
- `scripts/generate-backend-refresh-token-migration-sql.ps1` generates reviewed SQL for a specific backend refresh-token migration flow.
- `ops/postgres/backup_lvt_postgres.sh` supports PostgreSQL backups.
- `tools/install_postgres_backup_schedule_commands.ps1` prints backup schedule installation commands.
- `tools/db_backup_restore_drill_commands.ps1` prints backup/restore drill commands.
- `tools/migration_rollback_remediation_commands.ps1` prints migration rollback/remediation drill commands.

### Release gates, smoke checks, and documentation checks

- `tools/run_desktop_release_gate.ps1` is the desktop release gate.
- `tools/audit_desktop_backend_boundary.ps1` checks the desktop/backend boundary.
- `tools/audit_interface_localization.ps1` checks interface localization coverage.
- `tools/audit_lesson_content.ps1` checks lesson content.
- `tools/smoke_desktop_backend_routes.ps1` checks desktop/backend routes.
- `tools/smoke_single_active_lesson_guard.ps1` checks single-active-lesson behavior.
- `tools/verify_cms_admin_server_readiness.ps1` verifies CMS/admin server readiness.
- `tools/validate_cms_published_snapshot_runtime.ps1` validates CMS published-snapshot runtime status.
- Python documentation/policy tests found under `tests/` include static website, Website CMS publish regression, marketing/SEO, AI model CMS policy, lesson behavior, CMS tutor id policy, and privacy consent checks.

### Existing naming/versioning policies observed

- Direct Windows versions are SemVer-compatible strings such as `0.1.36-tester.31`.
- `scripts/package-windows-inno-release.ps1` derives a numeric assembly version from the SemVer core, for example `0.1.36.0`.
- Direct installer filenames use `LanguageVoiceTutorSetup-<version>.exe`.
- Direct channel metadata uses `channel=direct-tester`, `architecture=win-x64`, and `updateMode=manual-confirmation`.
- Backend versions use backend-specific names such as `0.1.35-backend.80`.

## Store package identity questions still open

- Final Partner Center reserved product name.
- Store package identity name.
- Publisher display name and publisher ID/certificate identity.
- Whether Store and direct installs can coexist side-by-side on the same Windows account.
- Whether the Store package should use a distinct app user model identity/display name for testing.
- Whether Store test flights/private audiences need a separate identity from the final public listing.

## Store package versioning plan

MSIX package versions must be numeric. Human product versions such as `0.1.36-tester.31` need a deterministic mapping to a numeric MSIX package version such as `0.1.36.0`.

Planned first-pass rule for prototype discussion only:

- Use the human product version for release notes and support diagnostics.
- Use the SemVer core as the first three numeric MSIX components.
- Use a reviewed fourth numeric component for Store upload sequencing.
- Do not publish a Store package until the fourth-component policy is confirmed.

Open question: whether tester suffix numbers like `tester.31` map to the fourth MSIX component, a CI build number, or Store-only package revision counter.

## Store-specific update behavior

The detailed audit and implementation plan is [`docs/WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md`](WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md). The Store build should rely on Microsoft Store update behavior. It should not check, download, cache, or launch the direct Inno installer from `latest.json`. The app may still display its human product version for support, but Store update availability should be owned by Store infrastructure rather than the direct website manifest. This behavior is not implemented yet; the recommended first follow-up is an explicit Store channel flag controlled by MSBuild/build configuration while keeping Direct as the default for the existing Inno flow.

## Store app data and session migration risks

MSIX package identity can change local app-data behavior. The current audit is documented in [`docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md`](WINDOWS_STORE_LOCAL_DATA_AUDIT.md). Keep the local-data migration decision pending until that audit is reviewed and the first Store-channel update-behavior boundary is planned.

Risks to test:

- Existing DPAPI-protected auth/session persistence may not be portable across identity/path changes.
- Local settings and lesson-history caches may be stored under direct-install paths.
- Store and direct installs sharing data could corrupt state if both are installed.
- Store and direct installs isolating data could sign users out or appear to lose local settings.
- Import-on-first-run could duplicate stale auth/session data if not designed carefully.

Audit recommendation for first prototype: start isolated from direct local data, do not manually copy access/refresh tokens, require fresh login if necessary, and rely on backend-owned account, entitlement, usage, and lesson history after sign-in. Final local-data migration remains pending until review.

## Store listing, legal, and asset checklist

Required before public submission:

- Partner Center account/product reservation.
- Store listing title, short description, long description, feature list, category, support contact, website, privacy policy, terms, and age-rating answers.
- Screenshots for supported desktop form factors following Microsoft Store requirements.
- Store logos/assets in required sizes.
- Clear disclosure that Android/iOS apps are planned but not currently available, if mentioned.
- No claim that the app is already available in Microsoft Store before approval/publication.
- No secrets, API keys, webhook secrets, connection strings, JWT secrets, raw provider payloads, or private customer/payment identifiers in listing material.

## Privacy, audio, account, AI, and payment disclosures

The listing and in-app Store build review should disclose, in plain language:

- Account sign-in is required for subscription/entitlement and usage tracking.
- The app records or processes microphone/audio input for language practice features when the learner uses voice functionality.
- AI features are provided through backend-mediated AI/STT/TTS services; the desktop app does not call OpenAI directly.
- Backend is the source of truth for account, subscription, entitlement, usage, and limits.
- Paddle/web checkout is the planned PC non-game billing approach, pending Partner Center disclosure and policy review.
- Microsoft Store payment/IAP is not implemented in this planning step.

Final payment wording must be reviewed against current Microsoft Store policies before submission. Do not state that Paddle is approved for the Store until Partner Center/policy review confirms the final wording and flow.

## Windows App Certification Kit requirement

After an actual MSIX prototype exists, run Windows App Certification Kit locally and record the command/process/results in the playbook. No WACK command is currently confirmed for this repository because no MSIX package project/prototype exists yet.

## Private/internal Store testing plan

1. Create a local MSIX prototype without touching the direct Inno installer.
2. Confirm package identity and version mapping.
3. Install on a clean Windows VM/test account.
4. Verify launch, sign-in, auth/session persistence, microphone permissions, TTS/STT flows, lesson start/finish, Settings, Account, and backend-only AI boundary.
5. Verify Store build does not use direct `latest.json` update flow.
6. Run Windows App Certification Kit.
7. Use Partner Center private audience/package flight only after local prototype and WACK notes are documented.

## Public Store submission plan

Public submission should happen only after:

- MSIX prototype is confirmed.
- WACK passes or failures are triaged.
- Store listing/legal/assets are complete.
- Paddle/web checkout disclosure wording is reviewed against Store policy.
- Direct and Store channel coexistence/data strategy is decided after reviewing `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md`.
- Rollback/fallback plan is documented.
- Direct Windows release channel remains independently usable.

## Rollback and fallback plan

- If Store certification fails, keep the direct Inno tester/direct download channel unchanged.
- If Store package has a critical bug before publication, do not publish; upload a corrected package version after validation.
- If Store package has a critical bug after publication, use Partner Center update/removal/visibility controls as appropriate and direct affected users to support.
- Do not attempt to repair a Store failure by changing direct `latest.json` or direct Inno update behavior.
- Backend rollback, database migration remediation, Website CMS/static site publish rollback, and direct installer rollback remain separate operational procedures.

## Gaps before MSIX prototype

- No confirmed MSIX packaging project yet.
- No confirmed Store package identity yet.
- No confirmed Store version mapping yet.
- Store channel/update behavior is planned in `docs/WINDOWS_STORE_CHANNEL_UPDATE_PLAN.md`, but no Store channel build flag is implemented yet.
- No confirmed Store update behavior implementation yet.
- No confirmed Store local-data migration strategy yet; `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md` recommends isolated first prototype behavior pending review.
- No Windows App Certification Kit command/process in the playbook yet.
- No Store screenshots/assets checklist completed yet.
- No Partner Center submission checklist completed yet.

## Next safe Codex tasks

- Task A: review `docs/WINDOWS_STORE_LOCAL_DATA_AUDIT.md` and confirm the first-prototype local-data decision.
- Task B: complete/review the Store channel/update behavior plan so Store builds cannot use direct `latest.json`.
- Task C: implement the Store channel flag/update behavior without changing direct Inno installer.
- Task D: prototype MSIX packaging locally without changing direct Inno installer.
- Task E: add Windows App Certification Kit local verification notes after the prototype.
- Task F: prepare Store listing/legal/assets content.
- Task G: decide final Paddle disclosure wording for Store listing and in-app upgrade flow.

## What must not be touched during Store preparation

- Do not change application code during this documentation step.
- Do not change backend code.
- Do not add migrations or database tables.
- Do not change Inno scripts or direct release artifacts.
- Do not remove, rename, or repurpose direct `latest.json`.
- Do not upload installers, deploy backend, publish Website CMS/static site, or create a Store submission.
- Do not add secrets or placeholder secrets.
- Do not implement payment changes or Microsoft Store IAP.
- Do not claim the app is already available in Microsoft Store.
- Do not claim MSIX packaging is implemented until a real package/prototype exists.
