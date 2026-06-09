# CMS/Admin Server Verification Runbook

Review date: 2026-06-09.

Public release is still not ready. This runbook prepares the production/server CMS/Admin connection foundation for Language Voice Tutor at `https://api.languagevoicetutor.com`. It does not change billing, Paddle, subscriptions, entitlements, password reset/change behavior, lesson JSON, or desktop runtime startup behavior.

## Current intent

- `/admin/` is the static Admin shell and may be reachable publicly as a web page.
- Every `/api/admin/...` endpoint must require authentication and bootstrap-admin authorization.
- CMS Content Admin endpoints currently keep their historical `/api/admin/dev/cms/...` paths for compatibility with the existing Admin shell, but they are bootstrap-admin protected and are available for server verification when `AdminBootstrap` is intentionally enabled.
- Runtime lesson content remains static JSON by default. The server must not serve CMS published snapshots at runtime unless `CmsContent__UsePublishedSnapshotForRuntime=true` is intentionally set.
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
CmsContent__UsePublishedSnapshotForRuntime=false
CmsContent__ContentPackSlug=static-json-v1
CmsContent__FallbackToStaticJson=true
ENV
sudo chmod 600 /etc/languagevoicetutor/backend.env
```

Replace `admin@example.com` with the registered server account that should be allowed into Admin. Keep `CmsContent__UsePublishedSnapshotForRuntime=false` during initial verification so static JSON remains the safe runtime source.

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

Expected source-status baseline while static JSON is still the runtime source:

```json
{
  "environmentName": "Production",
  "runtimeSource": "StaticJson",
  "readPublishedSnapshotEnabled": true,
  "usePublishedSnapshotForRuntime": false,
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

Expected: HTTP `200` for the admin identity, capabilities, CMS content packs, and runtime status. The runtime status should report `source=StaticJson` while `CmsContent__UsePublishedSnapshotForRuntime=false`.

Optional helper script from a workstation:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/verify_cms_admin_server_readiness.ps1 -BaseUrl https://api.languagevoicetutor.com
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/verify_cms_admin_server_readiness.ps1 -BaseUrl https://api.languagevoicetutor.com -AdminEmail admin@example.com -AdminBearerToken $env:EVT_ADMIN_BEARER_TOKEN
```

The helper does not contain secrets. Without tokens it verifies the public Admin shell, public non-secret CMS source diagnostic, and unauthenticated rejection. With an admin token it also verifies admin CMS status endpoints. With a non-admin token in `EVT_NON_ADMIN_BEARER_TOKEN`, it verifies authenticated non-admin rejection.

## CMS workflow verification

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
   - verify runtime status still reports `StaticJson` and the desktop still reads packaged static JSON unless runtime CMS is explicitly enabled.
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
    - keep `CmsContent__UsePublishedSnapshotForRuntime=false` for normal server operation during verification;
    - confirm `/api/cms/runtime-content/source-status` reports `StaticJson`;
    - confirm `/api/admin/dev/cms/runtime-content/status` reports static JSON unless the runtime flag is explicitly enabled.

## Intentionally enabling CMS published snapshots for runtime

Only after the published snapshot has been validated and a rollback owner is available, change the runtime flag:

```bash
sudo sed -i 's/^CmsContent__UsePublishedSnapshotForRuntime=.*/CmsContent__UsePublishedSnapshotForRuntime=true/' /etc/languagevoicetutor/backend.env
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
sudo sed -i 's/^CmsContent__FallbackToStaticJson=.*/CmsContent__FallbackToStaticJson=true/' /etc/languagevoicetutor/backend.env
sudo systemctl restart languagevoicetutor-backend
curl -fsS https://api.languagevoicetutor.com/api/cms/runtime-content/source-status | jq .
```

Expected rollback result: `runtimeSource=StaticJson`, `usePublishedSnapshotForRuntime=false`, and `fallbackToStaticJson=true`.

## EF migration status

No EF schema change is required for this server verification foundation. Existing CMS tables and migrations already cover content packs, topics, scenarios, prompt templates, tutor behavior profiles, audit logs, published snapshots, content versions, scenario definition JSON, and draft-save audit metadata. Run the pending-model check before deployment:

```bash
dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

Expected: no pending model changes.

## Deployment note

Deploy the backend build using the existing server deployment process. Do not upload generated `artifacts/`, installer files, release zips, or secrets. A typical publish/copy/restart sequence should be run from the deploy workstation or CI environment that already has server access configured; keep SSH keys and passwords outside this repository.

## First production CMS content-pack initialization

Production CMS/Admin login works when `AdminBootstrap__Enabled=true` and the signed-in account is in `AdminBootstrap__AdminEmails`. On a first production setup, the Admin CMS database may not yet contain the `static-json-v1` content pack/draft even though learner runtime is correctly reporting `runtimeSource=StaticJson`.

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

Runtime remains `StaticJson` until `CmsContent__UsePublishedSnapshotForRuntime=true` is intentionally enabled after separate validation/publishing. Keep `CmsContent__UsePublishedSnapshotForRuntime=false` as the safe default during production verification. No EF schema change is required for this initialization foundation.
