# Admin role assignment first-owner bootstrap runbook

This runbook is for a controlled manual validation operation only. It is not part of the default release gate, default build checks, or automatic smoke checks.

## Purpose

`tools/smoke_admin_role_assignment_bootstrap_first_owner.ps1` validates the first-owner bootstrap flow for persistent Admin role assignments. When the target database has no active persistent Admin users, the bootstrap endpoint creates only the first persistent Owner/SuperAdmin-equivalent mapping for the currently authenticated server-side user.

The operation must be run only against a known safe local or controlled test environment. Do not run it casually against production. Rollback or remediation must be handled carefully through database/audit-aware operations until full role management exists.

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

## Current limitations

Persistent roles are still not active in global authorization. AdminPermissionAuthorizationHandler still does not use persistent role read, safety, audit, write, actor, or bootstrap services. Existing BootstrapAdmin access remains the current controlled-tester runtime behavior for unmigrated Admin endpoints.

Admin UI role management still does not exist. There is no assign-role, disable-admin, create-admin, or invite flow in this runbook.
