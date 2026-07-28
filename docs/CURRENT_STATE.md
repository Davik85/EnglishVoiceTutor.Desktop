# Current State

Review date: 2026-07-27.

## 2026-07-27 accepted Google Play Billing foundation (repository only; not deployed)

Main contains the accepted disabled-by-default Google Play purchase-verification foundation through commit `335ef8a0`: authenticated token verification, SHA-256 purchase-token ownership fingerprinting, a sanitized subscriptions-v2 verifier, lazy ADC-backed dormant wiring, and internal verified ProductId/UTC period/acknowledgement/test-purchase metadata. A separate later repository change adds a dormant provider-scoped period persistence boundary: it requires an existing paid provider Subscription and only creates or extends an active `provider_event` Premium entitlement linked to that exact Subscription; it has no runtime caller, rejects test purchases, and is not connected to Google Play verification or Paddle processing. The public endpoint is `POST /api/me/billing/google-play/purchases/verify`; it remains `503 not_configured` by default and does not expose provider metadata. Migration `20260727045935_AddGooglePlayPurchaseClaims` exists but is unapplied in production.

This is repository state, not deployment evidence. A later dormant atomic Google Play ownership/Subscription/entitlement orchestration now uses the token fingerprint as the initial `google_play` ProviderSubscriptionId; it has no runtime caller, rejects test purchases, performs no acknowledgement, and creates no Payment or BillingEvent projection. Paddle checkout, webhook processing, Premium activation/cancellation/refund behavior, and Desktop Premium-expiry display remain the working website/Desktop path; Google Play must join, not replace, that shared entitlement source of truth. Production remains `0.1.35-backend.133`; the current backend release is `0.1.35-backend.133`, and commits after `.133` do not change that release claim. Account-status multi-provider selection, endpoint wiring, replacement-token reconciliation, RTDN, Mobile connection, credentials, sandbox testing, migration application, and deployment remain pending.

## 2026-07-23 production `.133` account-deletion completion verification

Production is `0.1.35-backend.133`, with `0.1.35-backend.132` as rollback. Commit `80396ce` is deployed; retained `AdminAction` target history no longer blocks execution, while real Admin/CMS dependencies and active paid access still block it. Migration `20260723045852_AddAccountAnonymizationExecution` is applied. A controlled production test created and processed an account-deletion request, completed anonymization, automatically resolved and redacted the request, replaced the original email with a unique `@deleted.invalid` address, prevented original-email lookup and new login, and prevented repeat execution. Refresh tokens are removed; an already-issued access token may remain usable until normal expiry, after which refresh fails and the client must clear the invalid session. This accepted expiry window is not an active backend defect and no further backend authentication change is planned. No Paddle/provider or financial record was modified. Public backend and database health both returned HTTP 200 `Healthy` after deployment. Account-deletion backend work is complete for the approved current scope; the active product focus returns to the Mobile client.

## 2026-07-23 production `.131` Admin confirmation report-ID correction

Production `0.1.35-backend.131` exposed a UI-only report-ID mismatch: the enabled deletion button passed a details response object whose identifier is `reportId`, while the confirmation guard read nonexistent `report.id`. The dialog therefore did not open and no execute request was sent. The correction passes the selected report ID explicitly through confirmation/execution and remains deployed in production `.133`; it changed no migration or backend execution contract.

## 2026-07-23 production `.130` account-deletion workflow correction

Production `0.1.35-backend.130` exposed a bounded workflow issue: support could manually mark an account-deletion request `resolved` while the preflight was blocked and anonymization had not run. The deployed correction rejects that manual transition until the related operation is completed and improves paid-period/Admin-CMS blocker guidance. No migration or Paddle/provider change was required.

## 2026-07-22 production account-deletion and Admin login-security release state

The active production backend is `0.1.35-backend.133`; `0.1.35-backend.132` is the previous rollback release. Account-anonymization Slice 1, both account-anonymization migrations, and the complete Admin preflight, confirmation, execution, and email-intake flow are deployed and production-verified. Email intake creates the same normal `account_deletion` support request with existing duplicate protection; it has no second-Admin approval and does not call Paddle or another provider. Active Premium blocks deletion until the paid period ends, with renewal cancellation communicated by the operator; refunds, disputes, and chargebacks remain manual support matters. Security commit `8dd301b3` (`Harden Admin login credential handling`) remains deployed. `languagevoicetutor-backend.service` is active and running; public `/health` returned HTTP 200 `Healthy`; and public `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`. CMS/Admin production smoke passed in a private window: Admin login, dashboard and Feedback & reports loading, account-deletion report opening, logout/repeat login, Sign in/Enter submission, legacy sensitive URL cleanup, and fail-closed native fallback verification.

The `.128` Admin login form explicitly posts to `/admin/` and its email/password inputs have no `name` attributes, so a missing or unparsable `admin.js` cannot serialize or transmit credentials through native form submission. Working JavaScript still prevents native submission and sends the trimmed email plus unchanged password as JSON to `POST /api/auth/login`. At startup, `admin.js` removes legacy `email` and `password` query parameters case-insensitively without reloading, while preserving the Admin path, unrelated query parameters, and hash. Removed values are not copied into fields, storage, logs, errors, or diagnostics. No migration, static website deployment, Website CMS publish, installer upload, billing/Paddle, role, or production configuration change was part of `.128`.

Authenticated `POST /api/me/account-deletion-requests` is deployed. It requires current-password confirmation, accepts an optional reason, derives identity from the authenticated learner, never persists or exposes the password, and permits only one unresolved request per user. Migration `20260721120000_AddActiveAccountDeletionRequestConstraint` is applied and adds only partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`; it created no table or sequence, so no additional grants or ownership changes were required.

The request uses the existing support queue and Admin email reply workflow. It does not automatically delete, deactivate, anonymize, revoke all token families, cancel subscriptions, or alter user data. Actual deletion/anonymization is the separately executed deployed Super-Admin workflow, and a request resolves only after that operation completes. Mobile Settings integration is implemented and manually verified: it requires the current password, safely rejects an incorrect password without creating a request or logging the learner out, and shows the returned request ID and status. See [Account-deletion requests](ACCOUNT_DELETION_REQUESTS.md).

## Progress V1

Authenticated backend-owned Progress V1 is available at `GET /api/me/progress`. It aggregates only owned finished sessions with non-null `finishedAt`, uses UTC calendar rules, and is separate from the maximum-50 Lesson History response. No schema, migration, index, stored aggregate, cache, job, or backfill was added. See [Progress Endpoints](PROGRESS_ENDPOINTS.md).

## Source of truth for current versions

These docs record the release-ready handoff state, but live systems can change. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

If a PowerShell path reads raw manifest text and `ConvertFrom-Json` fails because a UTF-8 BOM is present at the start of `latest.json`, strip the BOM before parsing:

```powershell
($raw -replace "^\uFEFF", "") | ConvertFrom-Json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Check production backend health and database health:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS. Generated release outputs, including `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, installers, and packages, must not be committed.

## Windows client functionality source of truth

For the current Windows desktop client feature baseline, language counts, lesson flow, settings sections, and mobile-client reuse notes, see [Windows Client Functionality Overview](WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md).

## Concise release-readiness status

- Backend: production is deployed and healthy at `https://api.languagevoicetutor.com` on `0.1.35-backend.133`, with `.132` as rollback; the execution migration and complete Admin confirmation/execution flow are deployed and production-verified. Account-deletion backend work is complete for the approved current scope.
- Website: public pages at `https://languagevoicetutor.com` are generated and Paddle-review polish is completed for the current static site.
- Download: the current Windows direct public release is visible without JavaScript when the local/public manifest is available and remains manifest-driven with JavaScript through `/releases/windows/direct/latest.json`.
- Windows installer: current Windows direct public release is `1.1`, installer `LanguageVoiceTutorSetup-1.1.exe`.
- AI Models: persistent production storage at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` is verified, survived a backend service restart, and contains the known-good `gpt-5.5` / `gpt-5.2` production setup.
- Billing: controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation are completed for the 2026-07-02 owner-led test; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; broad public paid launch remains pending final release-readiness review.
- Legal: pricing, subscription terms, terms, privacy, refunds, cancellation, support, seller/company details, AI/data disclosure, platform availability/status, and download pages are ready for owner/legal final review as product/legal drafts, not final legal advice.

Remaining follow-ups after Windows Direct Release 1.1 publication:

1. Code signing remains deferred and accepted as a known release risk / SmartScreen warning source for this release.
2. Post-release monitoring and customer feedback triage remain ongoing.
3. Backup/restore/rollback currency checks remain ongoing operational work.
4. Final owner/legal/support/pricing review remains a follow-up for broader marketing and paid-launch expansion.
5. Logging/release-readiness checks for remaining Admin operations and paid-launch evidence remain follow-up work.
6. Admin auth audit first production slice is complete for `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied`; session expiration audit persistence remains pending until separately implemented/verified.
7. Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal validation, and full-refund Premium revocation are completed; chargeback remains implemented/test-covered but not live-chargeback-tested; partial refunds remain conservative/manual-review; expanded customer portal/subscription management is deferred and not a 1.0 blocker; broad paid launch remains pending final release-readiness review.
8. Microsoft Store/MSIX was evaluated and discontinued for now; Microsoft Store availability is not claimed.

Do not state that the product is fully public production-ready. The current Windows release remains a public Windows direct release, not a full broad production-readiness claim, and not broad public production readiness.

## Backend deployment state and boundaries

Production backend URL: `https://api.languagevoicetutor.com`.

Health endpoints:

- `https://api.languagevoicetutor.com/health`
- `https://api.languagevoicetutor.com/api/health/database`

Current backend release is `0.1.35-backend.133`; `0.1.35-backend.132` is the previous rollback release. The deployed account-deletion flow includes migrations `20260722132656_AddAccountAnonymizationPreflightFoundation` and `20260723045852_AddAccountAnonymizationExecution`, plus the complete Admin confirmation/execution workflow. Public backend and database health returned HTTP 200 `Healthy`, and the controlled deletion verification passed after deployment. Previous backend rollback reference must always be verified from `/opt/languagevoicetutor/backend/previous` before rollback.

Backend deployment uses:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.123 -PackageFirst -DryRun
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.123 -PackageFirst
```

The backend upload flow uses the uploaded `deploy-backend-release.sh` helper and `ssh -tt` for sudo restart/status when needed. Do not document old fragile inline bash deployment paths as the current flow.

Backend deploy is separate from Windows installer upload, static website publish, Website CMS publish, database migrations, provider/Paddle live changes, and AI Models data/config correction. Backend upload/package scripts do not apply EF migrations automatically. Database migrations remain a separate reviewed SQL/operator process only when schema changes exist. Backend deploy does not upload Windows installer files, does not publish public website HTML, does not change production billing/Paddle configuration, and must not treat release-folder AI Models JSON as the production source of truth.

Admin Product Statistics still uses the `Tracked signed-in app/device records` label for backend `DeviceEntity` records; this metric is not raw installer downloads. `Successful payments total` and `Successful payments current month` remain internal billing-event metrics and are not the source of Premium access.

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Production Admin RBAC / persistent role management is completed for backend `0.1.35-backend.108`: persistent AdminUsers can sign in to `/admin`, admin source is reported as `persistent_role_assignment`, role-aware Admin UI works, `super_admin` can assign/revoke roles and disable AdminUsers, disabled AdminUsers lose Admin access, support and billing_support least-privilege checks passed, `403` from role-limited workflows no longer logs the admin out, and `401` still returns to login. Bootstrap Admin fallback for Admin permission policies remains disabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. The Website CMS endpoints are still authenticated/authorized but no longer consume the normal admin read/write rate limit because long legal text editing caused `RateLimitExceeded` during normal CMS work.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling is active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.


## 2026-07-19 authenticated Lesson History production state

Authenticated `GET /api/me/lesson-history` and `GET /api/me/lesson-history/{sessionId:guid}` are implemented, verified, deployed, and available in backend `0.1.35-backend.123`. Both require authentication and derive ownership exclusively from the authenticated request identity. The list returns only the learner's recent sessions, newest first, with a current maximum of 50 and summary, message-count, and valid-turn indicators. Detail returns owned session metadata plus available summary, transcript messages, and feedback; an unknown or non-owned session safely returns `404`, while unauthenticated requests return `401`. The canonical contract is [Lesson History Endpoints](LESSON_HISTORY_ENDPOINTS.md).

This completes the backend API, not Mobile History UI. Mobile must consume `/api/me/...`, never Desktop-local JSON or `/api/dev/...`. No Desktop behavior changed. The recent maximum-50 list is not an all-time Progress API: clients must not derive official totals, streaks, aggregates, or long-term progress from it. Future official Progress requires a separate backend-owned aggregate contract. No database migration or schema change was required.

## 2026-07-18 Website CMS inline Home-title typography production verification

Backend `0.1.35-backend.122` is production-deployed and healthy for the completed Website CMS inline Home-title typography feature and the Home-title font-size CSS precedence fix. The previous backend release is `0.1.35-backend.121`; `languagevoicetutor-backend.service` is active and running; public `/health` returned HTTP 200; public `/api/health/database` returned HTTP 200; no EF migrations were run; and no separate static-site upload was required for this backend release. Public page changes from title edits are applied later through Website CMS Publish.

Website CMS now edits Home page application-card title typography inline, directly below `windowsCardTitle` and `mobileCardTitle`. Windows and Mobile title styles are independent. Each title supports controlled font family, mobile size in pixels, desktop size in pixels, font weight, and line height through companion fields: `windowsCardTitleFontFamily`, `windowsCardTitleMobileSizePx`, `windowsCardTitleDesktopSizePx`, `windowsCardTitleFontWeight`, `windowsCardTitleLineHeight`, `mobileCardTitleFontFamily`, `mobileCardTitleMobileSizePx`, `mobileCardTitleDesktopSizePx`, `mobileCardTitleFontWeight`, and `mobileCardTitleLineHeight`. Safe defaults are heading-font inheritance, `28px` mobile size, `52px` desktop size, `800` weight, and `1.08` line height, so existing Website CMS JSON remains compatible and no database migration is required. Existing text, Design values, Marketing/SEO, legal pages, pricing, support, download content, and mobile-page content remain preserved.

The backend renderer generates responsive title size as `font-size: clamp(<mobileSize>px, 4vw, <desktopSize>px);`. CMS users do not edit raw CSS, `clamp()`, `vw`, selectors, or style attributes. The supported workflow is Home page title text plus inline **Text style** controls -> **Save draft** -> **Preview** -> **Publish / Make active**. There is no separate Typography tab, Typography page, global typography editor, raw CSS editor, or second Website CMS configuration system. Preview and Publish use the same backend renderer.

The first deployed implementation exposed a Preview-only symptom because existing public stylesheet selectors `.landing-page .app-panel h1` and `.landing-page .app-panel h2` had greater specificity than the generated class-only selectors, so font weight changed while font size stayed overridden. The final renderer-owned selectors are `.landing-page .app-panel h1.app-panel__title--windows` and `.landing-page .app-panel h2.app-panel__title--mobile`, which have sufficient normal CSS specificity. The fix did not introduce `!important`, inline styles, or JavaScript style assignment.

## 2026-07-17 Admin Feedback & reports production workflow

Backend `0.1.35-backend.119` is production-deployed and healthy for the complete Admin Feedback & reports workflow, with previous backend release `0.1.35-backend.118`. Repository commit `d4ddd33` (`Add admin feedback reports workflow`) was deployed through the standard `scripts/upload-backend-linux-release.ps1` flow with `PackageFirst`; a dry run completed before the real upload. The live backend `current` symlink was switched to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.119`, the `previous` symlink retained `.118`, `languagevoicetutor-backend.service` restarted successfully and was active/running, public `/health` returned HTTP 200, public `/api/health/database` returned HTTP 200, the Admin CMS loaded the deployed workflow, and no rollback was required. The deployment script did not run EF migrations.

The backend records authenticated user feedback through the existing `POST /api/me/feedback-reports` Mobile submission path. `UserId` remains backend-derived from authentication, supported categories remain `suggestion`, `app_issue`, and `ai_response`, and backend `.119` required no Mobile API contract change.

The Admin Feedback & reports API is deployed at `GET /api/admin/feedback-reports`, `GET /api/admin/feedback-reports/{reportId}`, `PATCH /api/admin/feedback-reports/{reportId}/status`, and `POST /api/admin/feedback-reports/{reportId}/replies`. The required permissions are `feedback_reports.read`, `feedback_reports.status.manage`, and `feedback_reports.reply`. Approved production roles are `super_admin` and `support`; these permissions are not granted to `content_editor`, `billing_support`, or `read_only_auditor`. Support does not gain unrelated Website CMS, legal content, billing/Premium, AI model settings, role management, secrets, or unrelated system-administration access.

Status workflow: `new` can be marked `reviewed` or `resolved`; `reviewed` can be resolved; `resolved` can be reopened as `reviewed`; manual reset to `new` is not supported; same-status updates are idempotent; and `ReviewedAtUtc` records the first review/resolution. A successful reply changes only `new` to `reviewed`, does not automatically resolve the report, and final resolution remains a deliberate Admin action.

Reply workflow: the recipient is resolved from the report user and cannot be changed by the Admin. The From address and the subject cannot be changed by the Admin; the subject is exactly `Language Voice Tutor support`. Reply attempts are persisted before delivery and use `pending`, `sent`, and `failed` states. Failed delivery does not change report status. Successful delivery changes only `new` to `reviewed`. Reply history is visible in report details, newest first, and failed attempts remain visible. No automatic retry, outbox, attachments, ticketing, reply editing/deletion, exports, bulk operations, or OpenAI processing was added.

Migration `20260717120148_AddUserFeedbackReports` previously added `user_feedback_reports`. Migration `20260717151432_AddUserFeedbackReportReplies` is now applied in production and added `user_feedback_report_replies`. The reply table exists, is owned by `lvt_app`, `lvt_app` has application access, and `lvt_analytics_reader` has no access to reply content. The migration was applied separately from backend deployment; no additional migration is required for backend `.119`. When reviewed SQL creates application objects under an operator role, ownership and grants must be verified before the migration is considered complete.

Production smoke validation confirmed an initial reply attempt failed safely because `SmtpEmail__Enabled` was absent: the reply text remained in the CMS, the failed attempt was stored in reply history, and the UI displayed a safe “Email delivery is not configured” message without exposing SMTP/provider details. Operators then added `SmtpEmail__Enabled=true` to the existing `/etc/languagevoicetutor/backend.env` without documenting SMTP host, username, password, From address, recipient address, or other secret values; after backend restart, both health endpoints remained HTTP 200. A second reply attempt was successfully delivered with the expected support subject and sender identity, the successful reply appeared in reply history, report status updated correctly, the report was subsequently resolved successfully, and reply history remained available.

The generic email sender selects the real SMTP transport only when all of these are true: `SmtpEmail__Enabled=true`, `Host` is configured, `Port` is greater than zero, and `FromAddress` is configured. Otherwise `NoOpEmailSender` is selected, `IsConfigured` is false, and no SMTP connection is attempted. Password reset and support replies use the same generic `IEmailSender` transport; only `SmtpEmailSender` contains SMTP transport logic. `PasswordResetEmailSender` remains a thin password-reset message formatter, and password-reset subject/body and external behavior remain unchanged. Safe failure logging uses fixed error categories and does not write raw provider exceptions, user IDs, token IDs, recipient emails, reset URLs, reset codes, token hashes, or SMTP details to the reviewed failure logs.

Admin CMS Feedback & reports is packaged inside the backend release, so no public static-site upload is required for this CMS change. List, filters, pagination, details, status controls, reply form, and reply history are deployed. Report and reply text are rendered as plain text. Reply drafts remain in memory only; failed sends preserve the current draft; successful sends clear it; switching reports clears the previous draft and history; and no reply data is stored in localStorage, sessionStorage, cookies, URLs, or console logs.

Final verification recorded for this workflow: backend build passed; focused Admin read tests passed; focused Admin CMS tests passed; focused email sender and safe logging tests passed; password-reset logging tests passed; Admin RBAC permission policy checks passed; Admin roles/permissions policy checks passed; the full desktop release gate with `IncludeEfChecks` passed; EF reported no pending model changes; the repository was pushed with a clean working tree before deployment; and production health, database health, email delivery, status changes, reply history, and report resolution were manually validated.

## 2026-07-13 backend 0.1.35-backend.116 production verification

Backend `0.1.35-backend.116` is production-deployed and healthy, with previous rollback release `0.1.35-backend.115`. The deployment used `scripts/upload-backend-linux-release.ps1 -Version 0.1.35-backend.116 -PackageFirst` after a completed dry run. Public `/health` returned HTTP 200 `Healthy`, public `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`, and `languagevoicetutor-backend.service` is active and running on `127.0.0.1:5001` behind the existing production setup. No EF migration was added or executed, no database schema changed, and no Desktop UI, Mobile, CMS UI, public website, Windows installer, billing, Paddle, voice, transcription, TTS, semantic-resolution, or AI Models configuration files were deployed or changed by this release.

The backend prerequisite for moving learner level selection into Mobile Settings -> Learning is complete: existing `GET /api/me/settings` returns `CurrentLevel`, and existing `PUT /api/me/settings` accepts optional `CurrentLevel` values `A1`, `A2`, `B1`, and `B2` with canonical uppercase storage/return behavior, new-profile default `A1`, preserve-on-omitted-or-null semantics, validation rejection for blank/unsupported PUT values, repair of legacy blank/whitespace and `unknown` to `A1`, and safe `A1` responses for arbitrary unsupported stored values such as `C1` without overwriting them. `UserProfileEntity.CurrentLevel` remains the storage location; no new endpoint, table, or migration was introduced.

The saved `CurrentLevel` is only the user's selected level and does not replace or duplicate CMS level behavior. Published CMS level profiles remain the source of truth for level-dependent lesson behavior and lesson length through the unchanged chain: saved `CurrentLevel` -> Mobile selects the matching runtime level profile -> backend runtime scenario supplies CMS-published `levelProfiles` -> the active level profile supplies language complexity, correction guidance, answer length, hint behavior, wrap-up timing, and final-turn timing -> lesson requests carry the selected level and resolved profile values -> `LessonLimitHelper` and `LessonPromptBuilder` apply those values. Mobile has not yet consumed `CurrentLevel`, the Choose Level start screen has not yet been removed, and physical Mobile validation of this new settings behavior has not yet happened.

## 2026-07-13 authenticated voice scenario resolution production verification

Backend `0.1.35-backend.115` was deployed and verified for this dated voice scenario structured-output fix; `0.1.35-backend.114` was the previous backend release for that deployment. This is historical context; the current production backend has since advanced to `0.1.35-backend.133`. Backend `0.1.35-backend.114` was already active before the `.115` deployment, so it must not be described as containing the `.115` structured-output fix. The `.115` deployment completed successfully through the existing `scripts/upload-backend-linux-release.ps1` flow, `languagevoicetutor-backend.service` was active, public `/health` returned HTTP 200, and public `/api/health/database` returned HTTP 200 with `canConnect=true`. No EF migration or database schema change was required or run. Website and Windows installer files were not deployed.

Backend `0.1.35-backend.115` fixes `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` returning HTTP 502 when the provider returned a structured-output shape that was permitted by the old provider schema but rejected by backend validation. The provider schema now has one explicit result shape for each decision: `published_context`, `free_context`, `clarify`, and `unsafe`. The backend converts the nested provider result back into the existing flat public endpoint response, so the public route and Mobile request/response contract did not change. `free_context` remains a first-class result, runtime candidate IDs are still validated against the current CMS candidates for the lesson, production credential validation remains unchanged, and the automated tests did not use a live OpenAI call. No scenario titles, transcript phrases, CMS scenario IDs, or language-specific production examples were added.

Verification recorded for this backend work: `OpenAiVoiceScenarioResolutionServiceTests` passed (`61 passed, 0 failed, 0 skipped`), the full backend test suite passed (`162 passed, 0 failed, 0 skipped`), the Release backend build succeeded with `0 warnings` and `0 errors`, production deployment completed through the existing upload script, production health/database-health checks passed, and no EF migrations were run. A physical Android retest is still required to confirm that the first clean voice scenario selection no longer returns HTTP 502. Initial mixed-script transcription rejection remains a separate Mobile issue; keyboard overflow and lesson-screen UI work remain separate Mobile issues; missing lesson-chat avatar assets remain a separate Mobile issue; and the complete Mobile voice flow must not be marked fully stabilized yet.

## 2026-07-11 authenticated lesson summary production verification

Backend `0.1.35-backend.112` was the current production backend for this dated verification; `0.1.35-backend.111` was the rollback release. It was packaged and uploaded with the normal Linux backend release scripts. Production verification confirmed the `current` symlink points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.112`, `languagevoicetutor-backend.service` is active, `/health` returns `200 Healthy`, and `/api/health/database` returns `200 Healthy` with `canConnect=true`. No EF migrations were run, no database schema changed, and Windows installer/release files stayed unchanged.

Backend `0.1.35-backend.111` already supported authenticated `PUT /api/me/lesson-sessions/{sessionId}/finish` and `GET /api/me/lesson-sessions/{sessionId}/summary`, but summary generation could remain unavailable because `LessonSummaryGenerationService` read only the top-level Responses API `output_text` field. Real provider responses may place the structured summary text under `output[].content[].text`; when top-level `output_text` was absent, an empty string reached JSON deserialization, causing a safely isolated `JsonException` that did not undo successful lesson completion.

Backend `0.1.35-backend.112` keeps top-level `output_text` support, adds fallback extraction from nonblank `output[].content[].text`, rejects blank provider output before JSON deserialization, and continues to isolate summary-generation failure from lesson completion. No local/client summary generation was introduced. Authenticated Finish triggers backend-owned generation; authenticated GET reads only the stored learner-safe result and does not regenerate a missing summary. Development `/api/dev/.../summary` routes remain diagnostic/development boundaries and are not the mobile production flow.

A real authenticated Flutter mobile lesson was verified in production on 2026-07-11: the session started, lesson messages persisted, `PUT /api/me/lesson-sessions/{sessionId}/finish` completed the lesson, and `GET /api/me/lesson-sessions/{sessionId}/summary` returned a ready backend-owned learner-safe summary displayed by mobile, including summary, strengths, improvements, vocabulary, grammar, and next steps. Authenticated desktop Finish uses the shared completion path, but desktop currently displays its existing local desktop summary flow; the `.112` extraction fix did not change desktop UI or Finish response contracts. Mobile is the first verified client displaying the authenticated backend-owned GET summary result.

## Production Admin RBAC / persistent roles

Production Admin RBAC / persistent role management is completed after backend release `0.1.35-backend.108`, deployed by the normal backend package/upload flow. The production backend `current` symlink was verified at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.108`; `/health` and `/api/health/database` returned `200 Healthy`. No EF migrations were added or run for this RBAC stage. Windows installer release files were not changed.

Manual production verification completed: persistent AdminUsers can sign in to `/admin`; admin source is `persistent_role_assignment`; the role-aware Admin UI works; role-limited workflows return `403` without logging the admin out; `401` still returns to login; `super_admin` can assign and revoke roles and disable AdminUsers; disabled AdminUsers lose Admin access; `support` can use allowed support workflows; `billing_support` can use Manual Premium Grant after selecting a user and providing a reason; `billing_support` cannot access `super_admin`-only areas; `support` cannot grant or revoke Premium; and role visibility/workflow availability matches the backend permission catalog.

Current final role policy:

- `support`: can sign in, use User Lookup / User Overview, read approved diagnostics and allowed audit entries, and reset free lesson allowance; cannot grant/revoke Premium, cancel paid renewal, manage roles, edit/publish CMS, edit Website, or manage System AI Models.
- `billing_support`: can sign in, use User Lookup / User Overview, read billing/subscription/Premium diagnostics, cancel paid renewal if the existing backend policy allows it, and Manual Premium Grant for verified payment recovery cases; cannot Premium Revoke unless explicitly granted later, manage roles, edit/publish CMS, edit Website, or manage System AI Models.
- `content_editor`: can use CMS content read/draft workflows according to current permissions; cannot publish/restore unless explicitly granted and cannot manage billing, Premium, Admin roles, or System AI Models.
- `read_only_auditor`: can use read-only diagnostics/audit/statistics according to current permissions; cannot mutate user, billing, Premium, CMS, Website, roles, or System AI Models.
- `super_admin`: has full Admin access, including role management, disabling AdminUsers, Premium support actions, CMS/Website/System controls according to existing backend permissions.

Admin Activity first production slice is completed: the Admin Activity tab is visible and usable, includes `admin_actions`, `admin_role_assignment_events`, and the production-applied `admin_auth_audit_events` source, displays admin-entered reasons/notes where stored, and keeps `safeMetadataJson` separate from Admin note. Production verification has shown `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` rows in Admin Activity from `admin_auth_audit_events`; session expiration audit persistence and any Website/AI publish audit coverage not already represented in existing audit tables remain pending until separately implemented/verified.

## Public website and Website CMS

Public site: `https://languagevoicetutor.com`.

The Website CMS exists in the Admin Shell under **Website**. It is intentionally simple and informational only; it is not a full CMS. Access is Super Admin / Bootstrap Admin protected. Content is JSON/file-based at `site/content/website-content.json`, and that JSON document contains both active and draft content. Public static site output is `site/public`.

Website flow:

1. Admin Website tab loads draft/active content.
2. **Save draft** writes draft content.
3. **Preview** renders the selected page preview without publishing.
4. **Publish / Make active** promotes draft/active content and renders static HTML files.

Publish generates these public pages: `index.html`, `download.html`, `mobile.html`, `pricing.html`, `support.html`, `terms.html`, `privacy.html`, `refunds.html`, `cancellation.html`, `seller.html`, `ai-data.html`, and `status.html`.

The Website CMS editor is simplified for normal pages: Page title, Body markdown, SEO title, and SEO description. Home remains structured because it has landing cards/assets. Design is not treated as a normal Super Admin editing page.

Markdown rendering supports headings, bold, italic, bullet lists, numbered lists, markdown links, plain safe URLs, plain emails, and bare domains such as `Paddle.com`. Unsafe schemes such as `javascript:`, `data:`, and `vbscript:` must remain rejected or escaped.

## Website CMS Marketing / SEO and public crawler readiness

The Website CMS now includes a visible **Marketing / SEO** section. These settings are stored through the existing JSON/file-based Website CMS model; no database table, schema change, migration, backend secret, environment variable, or committed example JSON value is required for Google setup. Google Analytics, Google Ads, and Search Console values are optional public website configuration and must be entered only in Admin Website CMS when real owner-approved values exist. Do not put real Google IDs, conversion labels, Search Console tokens, script snippets, GTM container IDs, secrets, or placeholder example values into code, docs, env files, or committed JSON examples.

Marketing / SEO fields:

- Enable consent banner
- Enable analytics
- Google Analytics Measurement ID
- Enable ads tracking
- Google Ads ID
- Google Ads download conversion label
- Google Search Console verification token
- Enable llms.txt

Current safe CMS values before real Google setup:

- Enable consent banner: ON
- Enable llms.txt: ON
- Enable analytics: OFF until a real GA4 Measurement ID is available
- Google Analytics Measurement ID: empty until available
- Enable ads tracking: OFF until real Google Ads values are available
- Google Ads ID: empty until available
- Google Ads download conversion label: empty until available
- Google Search Console verification token: empty until Search Console property verification is started

Operator field guide:

- Google Analytics Measurement ID: Google Analytics → Admin → Data streams → Web stream for `languagevoicetutor.com` → Measurement ID. Expected format: `G-XXXXXXXXXX`. Do not paste the example placeholder into CMS.
- Google Ads ID: Google Ads → Goals / Conversions → selected website conversion action → Tag setup. Expected format: `AW-123456789`. Do not paste the example placeholder into CMS.
- Google Ads download conversion label: same Google Ads conversion action setup; the label is specific to the download conversion action.
- Google Search Console verification token: Search Console → add property for `https://languagevoicetutor.com/` → HTML tag verification. Copy only the value inside `content="..."`, not the full meta tag.
- Do not paste whole Google script snippets into any of these fields.
- Do not use GTM container IDs in the GA Measurement ID field unless the website code explicitly supports GTM later.

Website Publish now emits or maintains public HTML pages, `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Public generated pages include canonical URLs, meta descriptions, Open Graph tags, Twitter card tags, JSON-LD where appropriate, and SoftwareApplication JSON-LD for the Windows desktop app only. Public pages must not claim Android/iOS availability and must not claim Microsoft Store, Google Play, or App Store availability.

Consent and privacy readiness:

- The consent banner is controlled from Website CMS.
- Consent mode defaults to denied before user choice: `analytics_storage`, `ad_storage`, `ad_user_data`, and `ad_personalization` are denied.
- The banner supports Accept all, Reject non-essential, Manage choices, and a Privacy Policy link.
- Privacy Policy includes optional analytics, advertising, and cookie consent disclosure.
- The website remains usable when non-essential cookies are rejected.
- Google Analytics / Google Ads scripts must not be emitted when IDs are empty or tracking is disabled.

Final verification caveats:

- Public pages must not contain placeholder GA IDs such as `G-XXXXXXXXXX`.
- Public pages must not contain placeholder Ads IDs such as `AW-123456789`.
- Public pages must not include `googletagmanager.com/gtag/js` while IDs are empty.
- `download.html` should show current Windows installer details from `latest.json` when static release details are available.
- `robots.txt`, `sitemap.xml`, `llms.txt`, and `marketing-consent.js` should return `200`.

## Current public website readiness

The home page shows the logo, supported study language flags, a Windows desktop app card, and safe mobile wording. Home must not claim mobile apps are currently available and must not say “Mobile version coming soon”. The approved wording is: “Android and iOS apps are planned but are not currently available.”

The generated footer is shared across pages and has two rows:

- Primary: Privacy Policy, Terms of Use, Refund Policy, Cancellation, Support, Pricing.
- Secondary: Seller / Company Details, AI & Data Disclosure, Service Status.

`seller.html`, `ai-data.html`, and `status.html` are part of the public site and are linked from the footer.

The download page is manifest-driven and also useful without JavaScript. When the local/public manifest is available, it statically shows current release details instead of only showing Loading or Unavailable. It keeps `download.js` and `/releases/windows/direct/latest.json` support. The static non-JS fallback text is:

- “Current Windows direct release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

The Download page is a structured Website CMS release page for the Desktop app, not just a generic markdown page. The CMS controls the visible CTA page title, main CTA body markdown, SEO title, SEO description, and four structured feature cards. The feature-card keys are `featureCard1Label`, `featureCard1Title`, `featureCard1Description`, `featureCard1ImagePath`, `featureCard2Label`, `featureCard2Title`, `featureCard2Description`, `featureCard2ImagePath`, `featureCard3Label`, `featureCard3Title`, `featureCard3Description`, `featureCard3ImagePath`, `featureCard4Label`, `featureCard4Title`, `featureCard4Description`, and `featureCard4ImagePath`. Default screenshot paths are `/assets/images/download/quick-start.webp`, `/assets/images/download/topics.webp`, `/assets/images/download/guided-lesson.webp`, and `/assets/images/download/conversation.webp`; these are public website assets, not Windows release artifacts.

Current public Download page layout: the existing Windows desktop app release hero remains. The left CTA card shows eyebrow `WINDOWS DESKTOP APP`, the CMS page title as the main heading, CMS body intro text, current version and installer size, the **Download for Windows** button, manifest status line, and SmartScreen/support notes. The right side shows four CMS-driven feature cards with screenshot images and accepted click-to-enlarge lightbox behavior. The footer follows the hero directly. There is no visible Technical release details block and no separate below-hero support card. `bodyMarkdown` is split visually: intro paragraphs render before version/button, SmartScreen/support-like notes render after manifest status, and obsolete “Current version details are loaded from the release manifest” text must not be shown as a public user-facing block.

`download.js` reads `/releases/windows/direct/latest.json`; version and installer size are manifest-driven. The safe non-JavaScript fallback download href is `/releases/windows/direct/LanguageVoiceTutorSetup-1.1.exe`. Do not reintroduce the old broken relative fallback `LanguageVoiceTutorSetup-1.1.exe`. The Download button must keep working if JavaScript or manifest loading fails by using the safe public installer fallback.

Accepted visual state: the Download page background is lightened to be closer to the Home page tone, cards use a readable blue-tinted translucent panel treatment, the CTA layout order is accepted, and feature-card lightbox behavior is accepted. Future visual changes should be small and scoped to Download page CSS unless explicitly requested.

## Historical Windows Direct Release 1.0 publication record

Windows Direct Release 1.0 was published on the public direct channel before the current `1.1` release. The release was built locally with Inno Setup, validated, uploaded to `/var/www/languagevoicetutor/releases/windows/direct`, verified on the server, verified over public HTTPS, verified on the website download page, and manually checked by downloading the installer from the public Download button.

Public release manifest values verified over HTTPS:

- `version`: `1.0`
- `installerFileName`: `LanguageVoiceTutorSetup-1.0.exe`
- `channel`: `direct-public`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `minimumSupportedVersion`: `1.0`
- `updateMode`: `manual-confirmation`
- `sha256`: `d6be93fbcd75536a0cd149bd8872c8327fc3131ede247b1db2b2d33d673680e1`
- `installerSizeBytes`: `188751650`

Publication verification completed:

- Local installer created: `artifacts\installers\windows\LanguageVoiceTutorSetup-1.0.exe`.
- Server-ready release copy created: `artifacts\releases\windows\direct\LanguageVoiceTutorSetup-1.0.exe`.
- Direct release manifest created: `artifacts\releases\windows\direct\latest.json`.
- `scripts\validate-windows-direct-release.ps1` passed before upload.
- `latest.json`, `changelog.json`, and `known-issues.json` parsed as JSON.
- `latest.json`, `changelog.json`, `known-issues.json`, and `checksums.sha256` had no UTF-8 BOM.
- Manifest identity matched product `Language Voice Tutor`, app id `LanguageVoiceTutor.Desktop`, platform `windows`, architecture `win-x64`, backend `https://api.languagevoicetutor.com`, channel `direct-public`, and update mode `manual-confirmation`.
- Installer SHA-256 matched both `latest.json` and `checksums.sha256`.
- `changelog.json` and `known-issues.json` both referenced version `1.0`.
- Dry-run upload uploaded nothing; the real upload completed successfully for `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and `LanguageVoiceTutorSetup-1.0.exe`.
- Server command confirmed remote `latest.json` content.
- Public HTTPS `latest.json` returned version `1.0`, installer `LanguageVoiceTutorSetup-1.0.exe`, backend `https://api.languagevoicetutor.com`, minimum supported version `1.0`, and update mode `manual-confirmation`.
- Public download page showed Current version `1.0`, release details for channel `direct-public`, size `180.0 MB`, and SHA-256 `d6be93fbcd75536a0cd149bd8872c8327fc3131ede247b1db2b2d33d673680e1`.
- Manual website check confirmed the Download button downloads the `1.0` installer.

Historical scope boundary: the public release upload affected only Windows direct release files. It did not deploy backend code, run migrations, modify database state, change billing/Paddle/refund logic, upload website files, rebuild the installer, change secrets, or change installer binaries. That historical Windows release upload did not change the backend; the backend has since advanced and the current production backend is `0.1.35-backend.128`. Code signing remains deferred and accepted as a known release risk for this release; Windows SmartScreen warnings remain expected until a future signed installer is published. The next public direct version after `1.1` should be `1.2`; future public direct versions should continue as `1.2`, `1.3`, and so on.

## Windows direct release

Manifest: `https://languagevoicetutor.com/releases/windows/direct/latest.json`.

Current public direct release values:

- `channel`: `direct-public`
- `version`: `1.1`
- `installerFileName`: `LanguageVoiceTutorSetup-1.1.exe`
- `installerRelativeUrl`: `LanguageVoiceTutorSetup-1.1.exe`
- `backendBaseUrl`: `https://api.languagevoicetutor.com`
- `updateMode`: `manual-confirmation`
- `minimumSupportedVersion`: `1.1`

The `1.1` Windows direct release has been built, uploaded, verified, and confirmed installed; the desktop displays version `1.1`. Backend deployment was not part of the desktop `1.1` release or the later static website upload; that desktop release did not change backend deployment; the current production backend is now healthy at `0.1.35-backend.128`, and no database migrations were added or run for either the `.112` summary extraction fix or the `.115` voice scenario structured-output validation fix. `minimumSupportedVersion` is intentionally `1.1` because `1.1` contains the desktop auth/session stability fix described below.


### Desktop auth/session fix in Windows Direct Release 1.1

Windows Direct Release `1.1` includes the desktop auth/session refresh bypass fix. Authenticated desktop clients that previously attached stale bearer tokens directly were converted to the central refresh-aware flow. The fixed clients include `BackendSubscriptionStatusClient`, `BackendCheckoutSessionClient`, `BackendCancelSubscriptionClient`, `BackendTrialClaimClient`, the authenticated `/me/settings` flow in `BackendUserSettingsClient`, and `BackendLessonAccessDecisionClient`. Expected behavior is that an expired access token with a valid refresh token refreshes, retries, and persists the replacement session instead of logging the user out. Update/reinstall should preserve the auth session, user settings, Lesson History, and Progress.

Release-relevant desktop polish included in historical `1.0`:

- Settings now includes a Contacts tab with `support@languagevoicetutor.com` and `https://languagevoicetutor.com`.
- Contacts is localized for all release-ready UI languages: `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Contacts uses the selected interface language, not the study language. The stale WPF binding state after interface-language changes was fixed by notifying Contacts bindings during interface-language refresh, so Russian Contacts text appears only for Russian UI and non-Russian UIs no longer show Russian Contacts text.
- Contact links are restricted to safe `https` and `mailto` handling.
- Situation/subtopic selection allows long localized topic names to wrap instead of clipping, and scenario card title/description wrapping remains protected by policy tests.
- Back during an unfinished active lesson now uses the same confirmation guard as Finish/End lesson: Cancel keeps the user in the lesson, Confirm continues the existing exit/end/navigation flow, and the guard does not apply before a lesson starts or after a lesson is already finalized.

Recent relevant implementation commits for this handoff state: `52b5c1a` (Polish desktop release localization and lesson guard), `c704ec3` (Fix contacts localization coverage), and `d2a1202` (Fix Contacts localization refresh).

Final local validation before/around this release included clean `git status`, `git diff --check`, `dotnet restore`, Debug and Release `dotnet build`, `python .\tools\test_desktop_release_polish_policy.py`, `python .\tools\test_finish_lesson_confirmation_policy.py`, `powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1`, and `powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1`. The desktop release gate passed restore, Debug build, Release build, backend build, lesson content audit, interface localization audit, desktop backend boundary audit, tutor prompt policy, lesson behavior CMS ownership policy, admin/RBAC static policy checks, and desktop release smoke gate automated checks; EF checks were skipped because there were no schema-affecting backend changes. Windows direct release validation for historical `1.0` passed release directory/file presence, no UTF-8 BOM, JSON parsing, required manifest fields, production backend URL, manual-confirmation update mode, installer presence, installer SHA-256 agreement with `latest.json` and `checksums.sha256`, and matching `1.0` changelog/known-issues versions.

Use the Windows direct-release upload helper:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 1.1
```

Do not manually `scp` installer files when the script exists. Windows direct release upload is separate from backend deploy and static website publish. After upload, verify `latest.json`, `installerFileName`, `backendBaseUrl`, installer hash, and that the download page button downloads the same installer named by the manifest.

Code signing remains deferred. CMS published-snapshot runtime is active for published Windows direct lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## Paddle, legal, and subscription architecture

Live Paddle is not enabled yet. Do not change production Paddle environment values as part of documentation or website review work. Do not put real Paddle API keys, price IDs, client-side tokens, webhook secrets, raw payloads, signatures, customer IDs, transaction IDs, JWT secrets, database URLs, OpenAI keys, or other secrets in docs.

Paddle remains behind the backend/provider adapter. Desktop must not directly decide Premium and must not directly integrate with Paddle. The backend remains the source of truth for plan, subscription, entitlement, usage, and limits. Entitlement remains the source of Premium access; `PaymentEntity` is diagnostic payment history only and is not the source of Premium.

Desktop now and future mobile clients share one backend account, one backend database, one subscription/entitlement state, and one lesson history/progress source. Paddle is likely the first web/desktop provider, but the architecture must allow Apple and Google later for mobile. Do not introduce YooKassa, Russia-only billing assumptions, a full Paddle state mirror, or production Paddle activation in documentation-only updates.

Website/legal pages prepared for review include Pricing / Subscription terms, Terms of Use, Privacy Policy, Refund Policy, Cancellation Policy, Support, Seller / Company Details, AI & Data Disclosure, Platform Availability / Service Status, and Download. Legal texts are product/legal drafts and must not be described as final legal advice. Seller details are public business details only; do not publish passport/private personal data. `Paddle.com` bare domains are clickable via markdown/autolink rendering. The download page, footer, and legal/support pages are Paddle-review-ready pending final owner/legal review.


## Selected tutor settings API deployment

Backend commit `268681e` (`Add selected tutor to user settings API`) is deployed in production release `0.1.35-backend.109`; the previous backend release was `0.1.35-backend.108`. The deployed package was `LanguageVoiceTutor.Backend-linux-x64-0.1.35-backend.109.zip`, uploaded/deployed with the normal PackageFirst backend flow. The deploy script did not run EF migrations because no database migration was needed: `UserProfileEntity.SelectedTutorId` already existed in the EF model and existing migrations. Production verification after deployment confirmed `/opt/languagevoicetutor/backend/current` resolved to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.109`, `languagevoicetutor-backend.service` was active/running, `https://api.languagevoicetutor.com/health` returned `200 Healthy`, and `https://api.languagevoicetutor.com/api/health/database` returned `200 Healthy`.

`selectedTutorId` is no longer a known backend API gap. `/api/me/settings` is now the persisted account-state source for the selected tutor: `GET /api/me/settings` returns `selectedTutorId`, and `PUT /api/me/settings` persists the selected tutor when a valid `selectedTutorId` is supplied. `GET /api/tutor-options` remains the source for available tutor options. The backend validates supplied tutor IDs against `TutorAvatarOptions.All`, rejects invalid values, canonicalizes valid IDs, and persists them to `UserProfileEntity.SelectedTutorId`. Omitted or `null` `selectedTutorId` values preserve the existing selected tutor for backward compatibility. The existing settings fields (`nativeLanguage`, `studyLanguage`, `explanationLanguage`, `speechVoice`, `speechSpeed`, and `conversationModeEnabled`) continue to work separately, and `speechVoice` is not changed automatically when `selectedTutorId` changes.

## AI model settings in Super Admin CMS

AI model identifiers for backend runtime are managed through the Super Admin / Bootstrap Admin controlled **Admin → System → AI Models** CMS endpoint set. Backend runtime remains the source of truth for AI model selection: the Desktop app calls backend endpoints and does not choose OpenAI model IDs. The active and draft values are stored in JSON/file-based persistent server data at `site/content/ai-model-settings.json` resolved outside versioned backend release folders (for production, under the persistent `/opt/languagevoicetutor/backend/site/content/` tree rather than `/opt/languagevoicetutor/backend/current` or `/opt/languagevoicetutor/backend/releases/<version>`). Packaged defaults are only fallback/seed data; startup must not overwrite an existing published active file. API keys are not stored in CMS, no database table is used, and no EF migration was added. OpenAI API keys remain server environment secrets, especially `OPENAI_API_KEY`.

Production verification update: `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` now exists as persistent server data/config, was seeded from the current release only because the persistent file was missing, has mode `644`, and survived a `languagevoicetutor-backend.service` restart. `sha256sum` matched the current release copy exactly (`94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`), and post-restart checks confirmed the file still existed and contained `gpt-5.5` plus `gpt-5.2`. This resolved the AI Models persistence risk. Future backend deploys must not rely on `/opt/languagevoicetutor/backend/current/site/content/ai-model-settings.json` or any `/opt/languagevoicetutor/backend/releases/<version>/site/content/ai-model-settings.json` file as the production source of truth.

Current known-good AI model configuration: lesson tutor chat uses `gpt-5.5`; feedback/correction, lesson hints, and translation use `gpt-5.2`; speech-to-text uses `gpt-4o-mini-transcribe`; normal lesson chat TTS uses `tts-1`; Conversation Mode TTS uses `gpt-4o-mini-tts`; and Realtime voice uses `gpt-realtime`. These are model IDs only and do not include provider credentials.

Known-good production values are lesson tutor chat `gpt-5.5`, feedback/correction `gpt-5.2`, lesson hint `gpt-5.2`, and translation `gpt-5.2`. Publishing AI model settings affects new backend AI requests without a desktop release because the desktop continues to call backend endpoints and does not choose OpenAI model IDs. The Super Admin workflow is: Load AI Models → Edit draft → Save draft → Validate format → Test provider access → Review compatibility diagnostics → Publish / Make active only if relevant runtime diagnostics pass → run a small real lesson after publishing. Validate format checks syntax only and does not prove provider access. Test provider access performs provider-level checks using draft settings, does not publish settings, and uses safe dummy input rather than real lesson/user text. Audio and realtime roles may be `not_tested` when not covered by lightweight provider tests.

The `gpt-5.5` lesson tutor chat investigation found that `gpt-5.5` was available to the deployed OpenAI API key/project. The root cause was the request parameter `temperature`, not model unavailability. Safe provider diagnostics recorded `statusCode: 400`, `safeCategory: invalid_request`, `providerErrorType: invalid_request_error`, `providerErrorParam: temperature`, and `sanitizedProviderMessage: Unsupported parameter: 'temperature' is not supported with this model.` Minimal Responses API text, minimal structured output, and the lesson runtime shape without user content passed after `temperature` was omitted. Therefore `gpt-5.5` can be used for lesson tutor chat when backend runtime requests omit `temperature`.

Backend request-shape rule: for `gpt-5.5` lesson tutor chat runtime requests, omit `temperature`; for `gpt-5.2`, preserve existing behavior and still send `temperature: 0.3` where currently configured. Do not reintroduce `temperature` for `gpt-5.5` unless provider compatibility changes and is retested, and do not assume newer model families accept every parameter accepted by older models. New model families must be tested with provider access diagnostics before publish.

Compatibility diagnostics are interpreted as follows: `minimal_responses_text` verifies basic model availability and Responses API access; `current_provider_test_shape` verifies the older provider-test shape including `temperature` if present; `minimal_structured_output` verifies strict structured output support using a tiny safe schema; and `lesson_chat_runtime_shape_without_user_content` verifies lesson runtime request options/schema with safe dummy input. If the minimal text check fails, suspect project/key availability or alias usage. If minimal text passes but the current provider-test shape fails, inspect the added parameter. If structured output fails, schema/text-format compatibility is the issue. If structured output passes but lesson runtime shape fails, the lesson schema or runtime request shape is the issue. If the lesson runtime shape passes, the model is safe to try in a small real lesson.

Provider errors are mapped to safe categories. Super Admin sees only safe provider fields: `statusCode`, `safeCategory`, `providerErrorType`, `providerErrorCode`, `providerErrorParam`, and `sanitizedProviderMessage`. Logs may include safe runtime fields such as `operation`, `modelRole`, `configuredModelId`, provider status/category, and provider error type/code/param/message where available. Logs and Admin UI must not expose API keys, Authorization headers, raw provider response bodies, raw request bodies, full prompts, private user lesson text, environment values, or connection strings.

## Windows distribution channel

The active Windows distribution channel is the Direct EXE/Inno installer. The direct `latest.json` update flow remains active for update checks, installer download, verification, and installer launch.

Microsoft Store/MSIX was evaluated with a local prototype and is discontinued for now. Store/MSIX packaging is not implemented or active, no Store submission is planned, and Store-channel runtime behavior should not be reintroduced unless the product decision changes in a separate future effort. Future Windows trust work should focus on buying and integrating a code signing certificate for the direct EXE/Inno installer.

Backend deploy, Website CMS/static site publish, Windows direct installer upload, and database migrations remain separate processes.

## 2026-06-30 release-readiness audit snapshot

### Current Active Release Strategy

- Windows: Direct EXE/Inno installer.
- Updates: direct `latest.json` manifest at `site/public/releases/windows/direct/latest.json` and `https://languagevoicetutor.com/releases/windows/direct/latest.json`.
- Signing: future trust work is a code signing certificate for the direct EXE/Inno installer.
- Backend: production API is `https://api.languagevoicetutor.com`; backend deploy uses package/upload helpers plus `/health` and `/api/health/database` checks.
- Website: public site is `https://languagevoicetutor.com`; Website CMS/static-site publish is separate from backend deploy.
- Billing: Paddle/global provider-agnostic billing remains the target; controlled Paddle live validation is completed, while broad paid-launch readiness remains pending final review.
- Store/MSIX: discontinued for now and not an active release path.

### Current release point

- Windows direct release: `1.1`, verified from public `https://languagevoicetutor.com/releases/windows/direct/latest.json` with `channel=direct-public`, installer `LanguageVoiceTutorSetup-1.1.exe`, production backend URL, `minimumSupportedVersion=1.1`, and manual-confirmation update mode. The tracked repository `site/public/releases/windows/direct/latest.json` was not changed by this docs update.
- Backend release in tracked release docs: current production is `0.1.35-backend.133`, with `.132` as rollback; `/health` and `/api/health/database` are verified healthy. Backend .99/.108/.112 references are historical and not current production unless a section is explicitly documenting those older releases.
- AI Models persistent production file: verified at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; it survived backend service restart, matched the current release copy by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`, and contains lesson tutor chat `gpt-5.5`, feedback/correction `gpt-5.2`, lesson hint `gpt-5.2`, and translation `gpt-5.2`. For `gpt-5.5`, backend requests must omit `temperature`.

### What is ready, partial, and blocked

Ready for controlled tester use: direct Windows manifest/update flow, production backend health-check procedure, CMS published-snapshot runtime for lessons, verified persistent AI Models production storage, Website CMS draft/publish mechanics, and documented secret boundaries.

Partially ready: Windows public installer release because signing and wider smoke/feedback remain; website/legal pages because owner/legal final review remains; AI tutor quality because CMS content approval and tester feedback remain. Backend operations remain controlled/manual: current production is documented as `0.1.35-backend.128`, with deploys, health checks, database health checks, and migrations kept as separate operations.

Blocked before broad public paid release: code signing for the direct installer, direct installer clean-machine/update smoke, final website/legal/support/pricing approval, monitoring/privacy/release-readiness review, and explicit release decision after controlled tester feedback. Controlled Paddle live payment/Premium activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation are completed, but they are not a broad launch decision; chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, and expanded customer portal/subscription management is deferred.

### Must not be touched for this docs-only state

Do not change backend runtime code, desktop runtime code, database schema/migrations, Inno installer behavior, deployment scripts, Website CMS live content, backend deployment, Windows direct upload, Store/MSIX files, Paddle/OpenAI/AI Models runtime behavior, generated artifacts, signing private keys, or secrets as part of this documentation audit.

### Do not mix these operations

- Backend deploy is not Windows installer upload.
- Website CMS publish is not backend deploy.
- DB migration is separate and must be reviewed.
- Direct Windows installer upload is not Store/MSIX.
- Paddle live account/provider changes are not code deploy unless an approved backend configuration/code change is required.

## 2026-06-30 Paddle live checkout preparation state

Paddle approved the website, backend live checkout code is deployed in production `0.1.35-backend.108`, `/pay.html` and `/paddle.public.json` are published under the real nginx root, and live server-side Paddle config is present in `/etc/languagevoicetutor/backend.env`. The controlled 2026-07-02 live payment/webhook/Premium activation path completed for the expected Language Voice Tutor Pro monthly price, and desktop cancel-renewal behavior was verified. Windows direct release remains `1.0`; AI Models persistent storage is verified and untouched. Store/MSIX remains discontinued; active Windows distribution remains Direct EXE/Inno.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.108` and the 2026-07-02 controlled live payment/cancel-renewal validation:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.1`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- Controlled Paddle live payment validation completed on 2026-07-02 for Language Voice Tutor Pro. A real Paddle live payment completed for 14.99 EUR by Google Pay for customer email `11111@gmail.com`; Paddle status was Complete. Backend `0.1.35-backend.108` remained healthy afterward: production backend health returned `200 Healthy` and production database health returned `200 Healthy`. Backend logs showed live checkout transaction creation, webhook receipt for `subscription.created`, `subscription.activated`, and `transaction.completed`, successful payment persistence, reconciliation marking the completed transaction for activation, subscription snapshot processing, and entitlement activation with `ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`. Earlier `transaction.payment_failed` attempts were stored and safely processed with `ActivatedCount=0` / `AlreadySkippedCount=1`; they did not grant Premium. One transient PostgreSQL serialization failure occurred during subscription lifecycle snapshot processing; the retry policy retried it, the retry succeeded, and final snapshot processing completed with `FailedCount=0`. This is observed non-blocking retry evidence, not a failed payment flow.
- Desktop Premium visibility was confirmed after payment: Current tariff `Premium`, free lessons remaining `without limits`, Premium active until `8/2/2026`, and auto-renewal initially Active. Cancel-renewal verification also completed from the desktop flow: after cancellation, Desktop still showed Current tariff `Premium`, free lessons remaining `without limits`, Premium active until `8/2/2026`, and Auto-renewal inactive. This confirms cancellation disables future renewal while preserving paid Premium access until the paid period end when no refund exists. The later full refund removes backend Premium access.
- Controlled live payment, webhook delivery, payment persistence, subscription snapshot processing, entitlement activation, desktop Premium visibility, and desktop cancel-renewal behavior were completed and documented on 2026-07-02. Paddle full-refund Premium revocation is production-verified on backend `0.1.35-backend.108` using the already stored live `adjustment.updated` event; automatic future handling should use delivered `adjustment.created` / `adjustment.updated` notifications, with the operator reprocess command reserved for already-stored/legacy events only. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker; broad public paid launch remains pending final release-readiness review and remaining release blockers.

Static website upload command must target the real nginx root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, the 2026-07-02 live payment/Premium activation/cancel-renewal path, failed-payment non-activation, and full-refund Premium revocation can be reported as completed. Chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, expanded customer portal/subscription management is deferred, and `billingPaidLaunchReleaseComplete=false` continues until final release-readiness review and remaining non-billing blockers are closed.

Admin RBAC note: `productionRolesAvailable` now means persistent Admin role authorization is active with an explicit fallback cutover (`AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`). It is not a broad public-launch flag and does not override remaining paid-launch blockers. Production diagnostics show two active `super_admin` AdminUsers and fallback disabled; if this flag is false, check the explicit fallback configuration and cutover status before changing role assignments.

## Admin Activity / Audit Log first safe slice (read-only)

- Added the first actor-centric **Admin Activity** view as a read-only slice built from existing audit tables only: `admin_actions`, `admin_role_assignment_events`, and `cms_content_audit_logs`.
- Admin Activity now displays existing admin-entered reasons/notes where those values are already stored in the normalized audit rows, while keeping safe metadata in a separate column.
- The backend endpoint is `GET /api/admin/activity` and is protected by the existing audit-read policy.
- A later approved migration added the dedicated `admin_auth_audit_events` table/source for Admin auth/session events rather than overloading `admin_actions`, `admin_role_assignment_events`, or `cms_content_audit_logs`.
- On 2026-07-01, migration `20260701000000_AddAdminAuthAuditEvents` was applied to production after a fresh readable backup and SQL review. The production table exists, the owner was corrected to `lvt_app`, and `lvt_app` has table privileges.
- Production Admin Activity includes the `admin_auth_audit_events` source dropdown entry and shows verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events. Session expiration audit persistence remains pending.
- Website/AI publish audit may still be partial when the corresponding events are not already present in the existing audit tables.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02; failed payment attempts did not grant Premium; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred and is not a current blocker, and broad public paid launch remains pending.

## 2026-07-01 Admin Activity and emergency Premium revoke update

Historical note: the production backend for this Admin Activity update has since advanced to `0.1.35-backend.116`, with previous release `/opt/languagevoicetutor/backend/releases/0.1.35-backend.115`. No EF migration or database schema change was required for the `.115` voice scenario structured-output validation fix, and website/Windows installer files were not changed.

- Admin Activity is visible and usable in production and includes `admin_role_assignment_events` plus `admin_actions`, including `manual_premium_grant` and `manual_premium_revoke`.
- Admin Activity table usability was improved with a top horizontal scrollbar and wider Admin note column; Admin note/reason is visible where stored, and `safeMetadataJson` remains separate from Admin note.
- Admin Activity continues to be read-only and now resolves existing `admin_actions` actor app-user ids to matching persistent `admin_users` where possible, so `actorAdminUserId`, `actorUserId`, and source/action filters can find existing admin action rows such as Manual Premium Grant, Manual Premium Revoke, Free Lesson Reset, and Billing Cancel Renewal.
- Manual Premium Revoke is completed as an emergency `super_admin` backend entitlement/access-control action. It requires an admin reason, expires/revokes active Premium entitlement rows, including paid/provider-backed active Premium entitlements, and writes an `admin_actions` Admin Activity entry with safe metadata. After revoke, the selected user no longer has active Premium access.
- Emergency Premium Revoke does not mutate Paddle provider history, does not delete `PaymentEntity` records, does not fake Paddle webhook events, does not make payment history the Premium access source, and does not change Paddle webhook/payment activation rules. Cancel paid renewal remains a separate future-renewal cancellation action; paid subscription/provider state may show `cancellation_scheduled` and `cancelAtPeriodEnd=true` while backend Premium access can still be separately revoked by `super_admin` when needed.
- No EF migration was added for this update; existing entitlement and admin action fields support the emergency revoke/audit behavior.
- Controlled Paddle live payment/webhook/Premium activation, failed-payment non-activation, desktop cancel-renewal, and full-refund Premium revocation were completed on 2026-07-02. Chargeback remains implemented/test-covered but not live-chargeback-tested; partial refund remains conservative/manual-review; expanded customer portal/subscription management is deferred and not a current blocker. Session expiration audit persistence remains pending and must not be marked complete.


## Mobile lesson-session reply placeholder production verification (2026-07-08)

Production backend `0.1.35-backend.112` is deployed and verified for backend-owned authenticated lesson completion and summaries. Backend `.111` already supported authenticated Finish and Summary but could leave summaries unavailable because `LessonSummaryGenerationService` read only top-level Responses API `output_text`; `.112` also supports nested `output[].content[].text`, rejects blank provider output before JSON deserialization, and preserves successful lesson completion if summary generation fails. `PUT /api/me/lesson-sessions/{sessionId}/finish` remains backward compatible with the existing desktop payload `{ "validTurnCount": 1 }`, marks owned sessions complete idempotently, and makes a best-effort backend summary generation attempt from persisted lesson messages plus safe lesson/session metadata. `GET /api/me/lesson-sessions/{sessionId}/summary` is the authenticated learner-safe read route for an already persisted result and does not regenerate missing summaries; production clients must not generate or upload summary, strengths, improvements, vocabulary, grammar, or next steps. A real authenticated Flutter mobile lesson displayed a ready backend-owned GET summary result on 2026-07-11. Existing desktop Finish uses the shared completion path, desktop UI and Finish response contracts are unchanged, and desktop currently displays its existing local desktop summary flow. Existing `POST /api/lesson-chat/reply` and `POST /api/me/lesson-sessions/{sessionId}/messages` behavior is unchanged. Development `/api/dev/.../summary` routes remain diagnostic/development-only and are not production mobile contracts. Desktop and mobile continue to share the backend session, completion, history, progress, and summary source of truth.

Production backend `0.1.35-backend.110` is deployed and verified for the backend-only mobile lesson-session reply placeholder route. The live `/opt/languagevoicetutor/backend/current` symlink resolved to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.110`, `https://api.languagevoicetutor.com/health` returned `200 OK` / `Healthy` / `Production`, `https://api.languagevoicetutor.com/api/health/database` returned `200 OK` / `Healthy` / `canConnect true`, and `languagevoicetutor-backend.service` was active/running. No EF migrations were added or run.

The new route is `POST /api/me/lesson-sessions/{sessionId}/reply` with request body `{ "messageText": "..." }`. It authenticates the user, verifies session ownership, verifies active session state, checks existing limits where applicable, and for a valid active session intentionally returns controlled `409 Conflict` with `mobile_lesson_reply_not_implemented`. Blank `messageText` returns `400`, missing/not-owned sessions return `404`, inactive/ended sessions return the existing session-ended `409` payload, exceeded chat reply limits return the existing `429` free/rate-limit payload, and unavailable session storage returns the existing `503` storage-unavailable payload.

This is not real mobile AI chat yet. Mobile must not call the existing desktop-owned `POST /api/lesson-chat/reply` endpoint directly, must not send the large desktop-built `LessonChatRequest`, must not duplicate desktop prompt/runtime/scenario/turn logic, and must not call OpenAI directly. The old `/api/lesson-chat/reply` endpoint was not changed. The expected next implementation direction is backend-side hydration of lesson runtime/server-side context before enabling AI replies, while mobile continues to send only `sessionId` and `messageText` to the new route.

Pre-deploy verification passed: `dotnet test backend\EnglishVoiceTutor.Api.Tests\EnglishVoiceTutor.Api.Tests.csproj` passed `89/89`; `python .\tools\test_backend_linux_deployment_policy.py`, `python .\tools\test_backend_refresh_token_migration_policy.py`, `python .\tools\test_backend_refresh_token_policy.py`, and `python .\tools\test_desktop_release_backend_lock_policy.py` passed. This was docs/backend-route-only release scope and did not change desktop code, mobile repo code, billing, OpenAI/provider configuration, deployment scripts, Website CMS/static site content, voice, TTS, realtime, analytics, history, store setup, or installer release artifacts.

## Admin auth audit persistence production verification (2026-07-01)

- Migration `20260701000000_AddAdminAuthAuditEvents` was applied before backend `0.1.35-backend.108` deployment, after fresh backup creation and SQL review.
- Fresh pre-migration backup evidence is limited to safe metadata: path `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260701_154405Z.dump`, size `6.4M`, and `pg_restore --list` line count `245`. Do not paste backup contents, SQL dumps, secrets, env files, tokens, cookies, provider payloads, or raw user data.
- Production backend `0.1.35-backend.108` is deployed successfully; `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.108`, `languagevoicetutor-backend.service` is active/running, `/health` returns `200 Healthy`, and `/api/health/database` returns `200 Healthy`.
- The dedicated `admin_auth_audit_events` table exists in production, its owner was corrected to `lvt_app`, and `lvt_app` has table privileges.
- Admin Activity includes `admin_auth_audit_events` as a read-only source, the source dropdown includes `admin_auth_audit_events`, and production Admin Activity shows verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events.
- Session expiration audit persistence remains pending and is not claimed complete.
- Session expiration persistence remains pending; no low-noise expiration persistence completion is claimed.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.

## CMS capability and runtime production verification (2026-07-01)

Backend `0.1.35-backend.108` fixed the stale `cmsUiAvailable` capability state. **System → Capabilities Check** now shows `cmsUiAvailable` as AVAILABLE, the Admin Shell **CMS Content** tab opens, and the CMS Content workspace loads. This is production UI availability verification only; no CMS content was saved, published, restored, initialized, imported, or otherwise mutated during this verification.

Learner runtime is production-verified as using `CmsPublishedSnapshot`, with the CMS published snapshot active and valid. Runtime status currently shows content pack slug `static-json-v1`, published version number `46`, 6 topics, 26 scenarios, 4 prompt templates, 3 tutor behavior profiles, validation success `Yes`, and currently using static JSON fallback `No`. Static JSON remains available as emergency fallback, but it is not active in the verified production runtime state.

## 2026-07-02 refund and chargeback Premium protection

In production backend `0.1.35-backend.108`, full Paddle refunds are treated as access-control events after `adjustment.created` or `adjustment.updated` webhook processing: the backend preserves Paddle/payment/subscription history, maps the adjustment back to the internal user by safe metadata or existing payment/subscription records, and expires active provider-event Premium entitlements with reason `paddle_full_refund`. Chargebacks are implemented as stronger refund evidence and are covered by tests/fake paths, but no real live chargeback was performed.

Normal cancel-renewal behavior is unchanged: scheduled cancellation keeps Premium through the paid period end. Partial refunds are conservative in this slice: the event is safely recorded/processed for review and Premium is left unchanged unless the adjustment is full or a chargeback. Provider history is preserved; payment and subscription records are not deleted, and refund processing does not fake Paddle webhook events or expose raw provider payloads, webhook signatures, tokens, cookies, secrets, API keys, or full card/payment data in Admin Activity evidence.

Full-refund Premium revocation is production-verified on current production backend `0.1.35-backend.108`: the operator reprocess of stored provider event `evt_01kwhgmvh1v9k8ve70gvnfeskm` (`adjustment.updated`, transaction `txn_01kwhg9bdxhp5738wqwc7xkh3q`, subscription `sub_01kwhga8nbx7hdcqgq5fea9wc6`) returned `UserResolutionSource=payment`, `FullRefundDetected=True`, `ChargebackDetected=False`, `EntitlementCandidatesCount=1`, `RevokedCount=1`, `Result=Revoked`, and `BlockReason=(null)`. Admin User Lookup confirmed `planId=free`, `planName=Free`, `premiumActive=No`, and `trialActive=No`; Admin Activity showed `actionType=paddle_full_refund_premium_revoke`, `result=succeeded`, targeting the refunded user. Broad public paid launch is no longer blocked by full-refund revoke, but remains pending final release-readiness review and non-billing blockers. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.

## 2026-07-02 Paddle refund replay recovery status

Production backend `0.1.35-backend.97` was deployed and verified healthy: `/opt/languagevoicetutor/backend/current` pointed to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.97`, `/health` and `/api/health/database` returned healthy, `languagevoicetutor-backend.service` was active/running, and no EF migrations were run by the upload script. After deployment, replaying the already-delivered Paddle `adjustment.updated` event `evt_01kwhgmvh1v9k8ve70gvnfeskm` was idempotent: the provider event id was a duplicate, normalization reported `AlreadyNormalizedCount=1`, payment persistence reported `AlreadyCurrentCount=1`, reconciliation did not reprocess the existing already-normalized/skipped event, and entitlement activation reported `AlreadySkippedCount=1`. Premium remained active.

Root cause: backend `.97` fixed fallback user resolution for new adjustment events, but Paddle replay keeps the same provider event id. Existing events normalized/skipped or blocked under `.96` are not automatically replayed through the `received -> reconciliation_pending -> processed` pipeline by duplicate webhook ingestion.

Backend `0.1.35-backend.98` was deployed and healthy, and the operator-only command ran correctly through `systemd-run` with the backend environment file for `evt_01kwhgmvh1v9k8ve70gvnfeskm`, but it returned `Result=Blocked` / `BlockReason=reconciliation_blocked` even though it found the stored `adjustment.updated` event, resolved the user through payment history, detected a full refund, and found one active Premium entitlement candidate. Root cause: `.98` reprocess still depended on the old reconciliation pipeline/state for an event already blocked/skipped under older code. Backend `.99` fixed the explicit operator-only recovery path. The `.99` operator reprocess returned `Result=Revoked` for the stored full-refund event, and Admin/Desktop status confirmed Premium inactive. No more live payment, refund, replay, or chargeback testing is required for this release-readiness slice. Expanded customer portal/subscription management is deferred and is not a current blocker. Direct installer code signing remains pending.
