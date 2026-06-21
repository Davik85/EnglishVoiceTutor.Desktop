# Admin role assignment first-owner bootstrap runbook

This runbook is for a controlled manual validation operation only. It is not part of the default release gate, default build checks, or automatic smoke checks.

## Purpose

`tools/smoke_admin_role_assignment_bootstrap_first_owner.ps1` validates the first-owner bootstrap flow for persistent Admin role assignments. When the target database has no active persistent Admin users, the bootstrap endpoint creates only the first persistent Owner/SuperAdmin-equivalent mapping for the currently authenticated server-side user.

The operation must be run only against a known safe local or controlled test environment. Do not run it casually against production. Rollback or remediation must be handled carefully through database/audit-aware operations until full role management exists.

## Prerequisites

This runbook is not part of the normal desktop tester flow. The desktop tester flow is expected to use its configured release/test backend and does not require operators to run a local backend or local database.

Before running this smoke script:

- The target backend must already be running and reachable at the selected `-BaseUrl`.
- For a local backend, connection string `DefaultConnection` must be configured outside committed repository files before startup, for example via user secrets, `appsettings.Development.json`, or environment variables according to the project convention.
- If the local backend/database is intentionally not configured, do not run this smoke.
- Do not use production casually. Use only a known safe local or controlled test environment that is explicitly approved for this one-time operation.
- Do not commit local database connection strings, secrets, credentials, tokens, or real admin emails.
- Expect that the operation may create the first persistent `AdminUser` and Owner/SuperAdmin-equivalent role mapping in the target database.


## Safety flags

The script requires an explicit mutating confirmation flag:

```powershell
powershell -ExecutionPolicy Bypass -File tools/smoke_admin_role_assignment_bootstrap_first_owner.ps1 `
  -BaseUrl http://localhost:5000 `
  -AdminEmail <local-admin-smoke-email> `
  -AdminPassword <local-admin-smoke-password> `
  -ConfirmCreateFirstOwner
```

If `-ConfirmCreateFirstOwner` is missing, the script fails before making any HTTP call. The script defaults only to `http://localhost:5000` and rejects production-looking or non-local URLs unless `-AllowProductionUrl` is also supplied. Do not use `-AllowProductionUrl` unless the environment is explicitly approved and safe for this one-time operation.

The script does not contain credentials, tokens, secrets, or real admin email addresses. Operators must provide local smoke credentials at invocation time.

## Request body boundary

The bootstrap request body must contain only:

- `reason`
- `safeMetadataJson`

The request body must not contain `appUserId`, `normalizedEmail`, `email`, `targetAdminUserId`, `actorAdminUserId`, `actorRoleIds`, `roleId`, or any equivalent role/actor identity field. The endpoint derives the app user id and optional trusted email from server-side authenticated claims.

## Manual validation flow

The script performs this sequence after the confirmation and environment checks pass:

1. Authenticate with the existing local admin smoke-test login pattern.
2. Call `GET /api/admin/role-assignments/actor` before bootstrap.
3. Call `POST /api/admin/role-assignments/bootstrap-first-owner`.
4. Call `GET /api/admin/role-assignments/actor` after bootstrap.
5. Call `GET /api/admin/role-assignments/diagnostics` after bootstrap.
6. Print safe top-level result fields only.


## Troubleshooting

### `Connection string 'DefaultConnection' is required.`

This error means the local backend was started without the required local database configuration. Configure `DefaultConnection` outside committed files, then restart the backend before running the smoke script.

This does not mean the desktop app, the controlled tester release backend, or the Windows release package is broken. It only means the optional local backend used for this special manual bootstrap smoke is not configured.

## Current limitations

Persistent roles are still not active in global authorization. AdminPermissionAuthorizationHandler still does not use persistent role read, safety, audit, write, actor, or bootstrap services. Existing BootstrapAdmin access remains the current controlled-tester runtime behavior for unmigrated Admin endpoints.

Admin UI role management still does not exist. There is no assign-role, disable-admin, create-admin, or invite flow in this runbook.
