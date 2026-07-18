# CMS/Admin Server Verification Runbook

Review date: 2026-07-18.

Public release is still not ready. This runbook prepares the production/server CMS/Admin connection foundation for Language Voice Tutor at `https://api.languagevoicetutor.com`. It does not change billing, Paddle, subscriptions, entitlements, password reset/change behavior, lesson JSON, or desktop runtime startup behavior.

## Current intent

- `/admin/` is the static Admin shell and may be reachable publicly as a web page.
- Every `/api/admin/...` endpoint must require authentication and bootstrap-admin authorization.
- CMS Content Admin endpoints currently keep their historical `/api/admin/dev/cms/...` paths for compatibility with the existing Admin shell, but they are bootstrap-admin protected and are available for server verification when `AdminBootstrap` is intentionally enabled.
- Runtime lesson content is intended to use the CMS published snapshot when it is enabled, valid, and effectively active. Static JSON remains an emergency fallback and initialization source; if fallback is active in Admin CMS, treat it as an attention state.
- The public diagnostic endpoint `/api/cms/runtime-content/source-status` is non-secret and reports only environment/source flags and the configured content pack slug. It must not expose prompts, lesson content, user data, tokens, emails, database strings, SMTP settings, OpenAI keys, Paddle keys, or any other secret.

## Server environment variables to add or verify

Edit `/etc/languagevoicetutor/backend.env` on the server and verify these keys. Use the real bootstrap admin email address, but never commit secrets or personal credentials to the repository.

Required for Admin/CMS server verification:

```bash
sudo install -m 600 -o languagevoicetutor -g languagevoicetutor /etc/languagevoicetutor/backend.env /etc/languagevoicetutor/backend.env.bak.$(date -u +%Y%m%dT%H%M%SZ)
sudo sed -i '/^AdminBootstrap__Enabled=/d;/^AdminBootstrap__AdminEmails__0=/d;/^CmsContent__ReadPublishedSnapshotEnabled=/d;/^CmsContent__UsePublishedSnapshotForRuntime=/d;/^CmsContent__ContentPackSlug=/d;/^CmsContent__FallbackToStaticJson=/d' /etc/languagevoicetutor/backend.env
sudo tee -a /etc/languagevoicetutor/backend.env >/dev/null <<'ENV'
AdminBootstrap__Enabled=true
AdminBootstrap__AdminEmails__0=admin@example.com
CmsContent__ReadPublishedSnapshotEnabled=true
CmsContent__UsePublishedSnapshotForRuntime=true
CmsContent__ContentPackSlug=static-json-v1
CmsContent__FallbackToStaticJson=true
ENV
sudo chmod 600 /etc/languagevoicetutor/backend.env
```

Replace `admin@example.com` with the registered server account that should be allowed into Admin. Keep `CmsContent__UsePublishedSnapshotForRuntime=true` and `CmsContent__ReadPublishedSnapshotEnabled=true` during verification so the CMS published snapshot is the primary runtime source; static JSON fallback remains available for emergency safety.

Also verify existing non-CMS production keys remain present and unchanged, including the database connection string, JWT signing key, OpenAI settings, password-reset SMTP settings, and any existing billing/Paddle keys. Do not paste those values into tickets, docs, scripts, or commits.

## Restart and health checks

After updating `/etc/languagevoicetutor/backend.env`, restart the backend and verify health:

```bash
sudo systemctl daemon-reload
sudo systemctl restart languagevoicetutor-backend
sudo systemctl status languagevoicetutor-backend --no-pager
journalctl -u languagevoicetutor-backend -n 100 --no-pager
curl -fsS https://api.languagevoicetutor.com/api/health
curl -fsS https://api.languagevoicetutor.com/api/health/database
curl -fsS https://api.languagevoicetutor.com/api/cms/runtime-content/source-status | jq .
```

Expected source-status baseline when CMS published snapshot is active:

```json
{
  "environmentName": "Production",
  "runtimeSource": "CmsPublishedSnapshot",
  "readPublishedSnapshotEnabled": true,
  "usePublishedSnapshotForRuntime": true,
  "fallbackToStaticJson": true,
  "contentPackSlug": "static-json-v1"
}
```

## HTTPS access checks

Public checks:

```bash
curl -I https://api.languagevoicetutor.com/admin/
curl -fsS https://api.languagevoicetutor.com/api/cms/runtime-content/source-status | jq .
```

Expected:

- `/admin/` returns HTTP `200` and serves only the static Admin shell.
- `/api/cms/runtime-content/source-status` returns HTTP `200` and only non-secret flags.

Unauthenticated Admin API checks:

```bash
curl -i https://api.languagevoicetutor.com/api/admin/me
curl -i https://api.languagevoicetutor.com/api/admin/capabilities
curl -i https://api.languagevoicetutor.com/api/admin/dev/cms/content-packs
curl -i https://api.languagevoicetutor.com/api/admin/dev/cms/runtime-content/status
```

Expected: HTTP `401` or `403`. No unauthenticated request may return Admin user data, CMS content, prompts, audit rows, versions, or runtime status details.

Authenticated Admin checks after signing in as the configured bootstrap admin and capturing a short-lived JWT locally:

```bash
export EVT_ADMIN_BEARER_TOKEN='<paste short-lived admin JWT only in your shell history-safe workflow>'
curl -fsS -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/me | jq .
curl -fsS -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/capabilities | jq .
curl -fsS -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/dev/cms/content-packs | jq .
curl -fsS -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/dev/cms/runtime-content/status | jq .
```

Expected: HTTP `200` for the admin identity, capabilities, CMS content packs, and runtime status. The runtime status should report `source=CmsPublishedSnapshot`, `validationSuccess=true`, and `fallbackUsed=false` when the published snapshot is valid. If static JSON fallback is active, treat it as an attention state.

Optional helper script from a workstation:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/verify_cms_admin_server_readiness.ps1 -BaseUrl https://api.languagevoicetutor.com
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/verify_cms_admin_server_readiness.ps1 -BaseUrl https://api.languagevoicetutor.com -AdminEmail admin@example.com -AdminBearerToken $env:EVT_ADMIN_BEARER_TOKEN
```

The helper does not contain secrets. Without tokens it verifies the public Admin shell, public non-secret CMS source diagnostic, and unauthenticated rejection. With an admin token it also verifies admin CMS status endpoints. With a non-admin token in `EVT_NON_ADMIN_BEARER_TOKEN`, it verifies authenticated non-admin rejection.

## Admin Feedback & reports production validation

Backend `0.1.35-backend.119` packages the Admin CMS Feedback & reports workflow inside the backend release. No public static-site upload is required for this Admin CMS change. The deployed workflow includes list, filters, pagination, details, status controls, reply form, and reply history. Report text and reply text are rendered as plain text. Reply drafts are in memory only: failed sends preserve the current draft, successful sends clear it, switching reports clears the previous draft and history, and no reply data is stored in localStorage, sessionStorage, cookies, URLs, or console logs.

Operational validation should confirm the production role boundary: `super_admin` and `support` can use Feedback & reports according to `feedback_reports.read`, `feedback_reports.status.manage`, and `feedback_reports.reply`; `content_editor`, `billing_support`, and `read_only_auditor` do not receive those permissions. Support must not gain unrelated Website CMS, legal content, billing/Premium, AI model settings, role management, secrets, or unrelated system-administration access.

Status controls must match the deployed workflow: `new` can be marked `reviewed` or `resolved`; `reviewed` can be resolved; `resolved` can be reopened as `reviewed`; manual reset to `new` is not supported; same-status updates are idempotent; `ReviewedAtUtc` records the first review/resolution; successful reply changes only `new` to `reviewed`; successful reply does not automatically resolve a report; and final resolution remains a deliberate Admin action.

Reply validation must not document recipient addresses or SMTP secrets. The recipient is resolved from the report user and cannot be changed by the Admin. From address and subject cannot be changed by the Admin, and the subject is exactly `Language Voice Tutor support`. Reply attempts are persisted before delivery with `pending`, `sent`, and `failed` states. Failed delivery does not change report status. Successful delivery changes only `new` to `reviewed`. Reply history is visible in report details, newest first, and failed attempts remain visible. No automatic retry, outbox, attachments, ticketing, OpenAI processing, reply editing/deletion, exports, or bulk operations exist.

Production smoke validation for backend `.119` recorded both the safe failure and the successful delivery path. First, with `SmtpEmail__Enabled` absent, a reply attempt failed safely: the reply text stayed in the CMS, the failed attempt was stored in reply history, and the UI showed “Email delivery is not configured” without SMTP/provider details. Operators then added `SmtpEmail__Enabled=true` to the existing `/etc/languagevoicetutor/backend.env` without copying SMTP host, username, password, From address, recipient email, or other secret values into documentation. After backend restart, `/health` and `/api/health/database` remained HTTP 200; a second reply was delivered with the expected support subject and sender identity; the successful reply appeared in history; the report status updated correctly; the report was resolved successfully; and reply history remained available.

The generic email sender uses real SMTP only when all requirements are true: `SmtpEmail__Enabled=true`, `Host` is configured, `Port` is greater than zero, and `FromAddress` is configured. If any requirement is missing, `NoOpEmailSender` is selected, `IsConfigured` is false, and no SMTP connection is attempted. Password reset and support replies share the same generic `IEmailSender` transport; only `SmtpEmailSender` contains SMTP transport logic; password reset formatting and external behavior remain unchanged; and reviewed safe failure logs must not contain raw provider exceptions, user IDs, token IDs, recipient emails, reset URLs, reset codes, token hashes, or SMTP details.

Database validation for the deployed workflow is separate from backend deployment. Migration `20260717151432_AddUserFeedbackReportReplies` is applied in production, `user_feedback_report_replies` exists, the table owner is `lvt_app`, `lvt_app` has application access, and `lvt_analytics_reader` has no access to reply content. No additional migration is required for backend `.119`.

## CMS workflow verification

### Website CMS Home-page title styles

Website CMS edits the two application-card title styles inline in **Website CMS → Home page**, directly below the `windowsCardTitle` and `mobileCardTitle` fields. The Windows and Mobile title styles are independent. Each title supports controlled font family, mobile size in pixels, desktop size in pixels, font weight, and line height; raw CSS is not supported.

Stored companion fields are `windowsCardTitleFontFamily`, `windowsCardTitleMobileSizePx`, `windowsCardTitleDesktopSizePx`, `windowsCardTitleFontWeight`, `windowsCardTitleLineHeight`, `mobileCardTitleFontFamily`, `mobileCardTitleMobileSizePx`, `mobileCardTitleDesktopSizePx`, `mobileCardTitleFontWeight`, and `mobileCardTitleLineHeight`. Missing fields receive safe defaults: inherited website heading font, `28px` mobile size, `52px` desktop size, `800` font weight, and `1.08` line height. Existing Website CMS JSON remains compatible, existing content sections are preserved, and no database migration is required.

The backend renderer owns the responsive CSS and emits `font-size: clamp(<mobileSize>px, 4vw, <desktopSize>px);`. CMS users do not edit CSS, `clamp()`, `vw`, selectors, or style attributes. The supported workflow is to edit the Home page title text and its inline **Text style** controls, click **Save draft**, use **Preview**, then **Publish / Make active**. Preview and Publish use the same backend renderer. There is no separate Typography tab, Typography page, global typography editor, raw CSS editor, or second Website CMS configuration system.

Operational CSS precedence note: the first deployed implementation used class-only generated selectors, but the existing public `.landing-page .app-panel h1` and `.landing-page .app-panel h2` selectors were more specific and kept overriding font size in Preview. The final renderer-owned selectors are `.landing-page .app-panel h1.app-panel__title--windows` and `.landing-page .app-panel h2.app-panel__title--mobile`, which have sufficient normal CSS specificity without `!important`, inline styles, or JavaScript style assignment.

Use `/admin/` with the configured bootstrap admin account.

1. Open `https://api.languagevoicetutor.com/admin/`.
2. Sign in as the registered bootstrap admin.
3. Open the CMS Content workspace.
4. Load `static-json-v1` content packs.
5. Verify the Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit tabs load.
6. Draft-save test:
   - choose a low-risk metadata field such as a topic description;
   - make a small reversible draft-only edit;
   - click **Save draft**;
   - verify the UI reports success;
   - verify Audit shows the draft save;
   - verify saved draft changes are not runtime-visible until publishing, and runtime status remains `CmsPublishedSnapshot` with `fallbackUsed=false` if the current published snapshot is valid.
7. Validation test:
   - run Validation & Preview;
   - verify validation passes and preview counts are plausible for `static-json-v1`.
8. Publish test:
   - go to Versions & Publish;
   - enter a clear publish summary;
   - publish the draft;
   - verify a new immutable version appears;
   - verify published-content status reports a valid `CmsPublishedSnapshot` with a valid hash and expected counts.
9. Restore/versioning test:
   - restore the previous known-good version only if the publish test intentionally changed content;
   - verify restore creates a new published version instead of editing the old immutable version;
   - verify audit records the restore/publish action.
10. Runtime snapshot safety test:
    - keep `CmsContent__UsePublishedSnapshotForRuntime=true` for normal server operation during verification;
    - confirm `/api/cms/runtime-content/source-status` reports `CmsPublishedSnapshot`;
    - confirm `/api/admin/dev/cms/runtime-content/status` reports `CmsPublishedSnapshot`, validation success, and `fallbackUsed=false`.

## Verifying CMS published snapshots for runtime

After the published snapshot has been validated and a rollback owner is available, verify the runtime flags are enabled:

```bash
sudo sed -i 's/^CmsContent__UsePublishedSnapshotForRuntime=.*/CmsContent__UsePublishedSnapshotForRuntime=true/' /etc/languagevoicetutor/backend.env
sudo sed -i 's/^CmsContent__ReadPublishedSnapshotEnabled=.*/CmsContent__ReadPublishedSnapshotEnabled=true/' /etc/languagevoicetutor/backend.env
sudo systemctl restart languagevoicetutor-backend
curl -fsS https://api.languagevoicetutor.com/api/cms/runtime-content/source-status | jq .
curl -fsS -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/dev/cms/runtime-content/status | jq .
```

Expected:

- public source-status reports `runtimeSource=CmsPublishedSnapshot`;
- admin runtime status reports `source=CmsPublishedSnapshot`, `success=true`, `validationPassed=true`, `hashValid=true`, and expected content counts;
- `fallbackUsed=false` on the happy path.

## Rollback to static JSON runtime

If CMS runtime content has any issue, immediately restore static JSON runtime:

```bash
sudo sed -i 's/^CmsContent__UsePublishedSnapshotForRuntime=.*/CmsContent__UsePublishedSnapshotForRuntime=false/' /etc/languagevoicetutor/backend.env
sudo sed -i 's/^CmsContent__ReadPublishedSnapshotEnabled=.*/CmsContent__ReadPublishedSnapshotEnabled=false/' /etc/languagevoicetutor/backend.env
sudo sed -i 's/^CmsContent__FallbackToStaticJson=.*/CmsContent__FallbackToStaticJson=true/' /etc/languagevoicetutor/backend.env
sudo systemctl restart languagevoicetutor-backend
curl -fsS https://api.languagevoicetutor.com/api/cms/runtime-content/source-status | jq .
```

Expected rollback result: `runtimeSource=StaticJson`, `usePublishedSnapshotForRuntime=false`, and `fallbackToStaticJson=true`; Admin CMS must show static JSON fallback as an attention state.

## EF migration status

No EF schema change is required for this server verification foundation. Existing CMS tables and migrations already cover content packs, topics, scenarios, prompt templates, tutor behavior profiles, audit logs, published snapshots, content versions, scenario definition JSON, and draft-save audit metadata. Run the pending-model check before deployment:

```bash
dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

Expected: no pending model changes.

## Deployment note

Deploy the backend build using the existing server deployment process. Do not upload generated `artifacts/`, installer files, release zips, or secrets. A typical publish/copy/restart sequence should be run from the deploy workstation or CI environment that already has server access configured; keep SSH keys and passwords outside this repository.

## First production CMS content-pack initialization

Production CMS/Admin login works when `AdminBootstrap__Enabled=true` and the signed-in account is in `AdminBootstrap__AdminEmails`. On a first production setup, the Admin CMS database may not yet contain the `static-json-v1` content pack/draft. Until a valid published snapshot exists, static JSON fallback may protect learners and must be treated as an attention state.

If the CMS Content overview says **"Content pack static-json-v1 has not been initialized in CMS yet."**, use the admin-only **Initialize from static JSON** action. It calls:

```bash
curl -fsS -X POST -H "Authorization: Bearer $EVT_ADMIN_BEARER_TOKEN" https://api.languagevoicetutor.com/api/admin/dev/cms/content-packs/static-json-v1/initialize-from-static-json | jq .
```

Expected behavior:

- the endpoint is protected by the existing `BootstrapAdmin` policy;
- it creates `static-json-v1` if the CMS content pack is missing;
- it imports the packaged static JSON topics, scenarios, prompt templates, tutor profiles, and available study-language metadata references into CMS draft/admin tables supported by the current schema;
- it preserves existing draft content instead of blindly overwriting it;
- it does not publish automatically;
- it does not switch runtime.

Runtime should use `CmsPublishedSnapshot` after validation/publishing with `CmsContent__UsePublishedSnapshotForRuntime=true` and `CmsContent__ReadPublishedSnapshotEnabled=true`. Static JSON remains the emergency fallback and initialization source. No EF schema change is required for this initialization foundation.
