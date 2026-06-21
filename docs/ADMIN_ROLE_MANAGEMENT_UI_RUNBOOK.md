# Admin Role Management UI Runbook

Review date: 2026-06-21.

Scope: controlled tester/admin operational validation only. This runbook is not a broad public production cutover, does not authorize casual production use, and does not replace the separate first-owner bootstrap runbook.

## Current status

- The Admin UI Role Management MVP exists as the **Persistent Admin Roles / Role Assignments** surface.
- The UI uses only the existing guarded role-assignment backend endpoints.
- The UI does not add backend endpoints, does not add EF migrations, and does not change Desktop, packaging, billing, Paddle, entitlement, lesson, CMS write, or release behavior.
- `POST /api/admin/role-assignments/bootstrap-first-owner` remains manual/runbook-controlled and is not exposed in the Admin UI.
- BootstrapAdmin fallback still exists during controlled rollout. Production Admin RBAC is not complete.

## Required preconditions

Before using the Admin UI Role Management MVP or the optional smoke script:

1. Backend is running in a known safe local or controlled tester/admin environment.
2. An admin login is available through the normal backend auth flow.
3. A persistent actor mapping exists for the authenticated admin.
4. The target app user exists before provisioning a persistent `AdminUser` mapping.
5. Every mutating role-management action has a non-empty human-readable reason.
6. Operators understand that role changes are audited and should be reviewed after validation.
7. Operators have reviewed the separate first-owner bootstrap runbook if no persistent owner/admin actor exists yet.

## Manual Admin UI checklist

Use placeholders or safe test users only. Do not use real production credentials in notes, screenshots, scripts, or issue comments.

1. Open the Admin UI in the controlled environment.
2. Sign in with an approved admin account.
3. Verify the **Role Assignments** navigation is visible under Persistent Admin Roles.
4. Open the Role Assignments section.
5. Verify the actor mapping panel loads and shows only safe summarized actor fields.
6. Verify the diagnostics panel loads and shows only safe summarized aggregate fields.
7. Confirm the UI does not expose `bootstrap-first-owner` anywhere.
8. Confirm the UI does not show raw access tokens, refresh tokens, cookies, passwords, provider secrets, raw provider payloads, or connection strings.
9. Provision an `AdminUser` only for a safe test app user id that already exists.
10. Assign a role to a safe test admin user with a clear reason.
11. Revoke the same role with a clear reason.
12. Disable the safe test admin user with a clear reason.
13. Enable the safe test admin user with a clear reason.
14. Verify success and failure messages are safe summaries and do not include raw response bodies or secrets.
15. Review audit/database state according to the controlled validation plan.

## Optional smoke script

Script location:

```powershell
.\tools\smoke_admin_role_management_flow.ps1
```

The script defaults to `http://localhost:5000`, rejects production-looking or non-local URLs unless `-AllowProductionUrl` is supplied, and runs read-only checks unless `-ConfirmRoleManagementMutations` is supplied.

### Read-only example

```powershell
.\tools\smoke_admin_role_management_flow.ps1 `
  -BaseUrl "http://localhost:5000" `
  -AdminEmail "<ADMIN_EMAIL>" `
  -AdminPassword "<ADMIN_PASSWORD>"
```

Read-only mode logs in, calls the current actor endpoint, calls diagnostics, and prints safe summarized fields only.

### Mutation-enabled example

Use only against a safe controlled environment with placeholder values replaced at runtime. Do not paste real credentials into committed files or shared docs.

```powershell
.\tools\smoke_admin_role_management_flow.ps1 `
  -BaseUrl "http://localhost:5000" `
  -AdminEmail "<ADMIN_EMAIL>" `
  -AdminPassword "<ADMIN_PASSWORD>" `
  -TargetAppUserId "<TARGET_APP_USER_ID>" `
  -TargetAdminUserId "<TARGET_ADMIN_USER_ID>" `
  -RoleId "<ROLE_ID>" `
  -Reason "<HUMAN_READABLE_REASON>" `
  -SafeMetadataJson '{"source":"manual_role_management_validation"}' `
  -ConfirmRoleManagementMutations
```

Mutation-enabled mode runs only guarded sections whose required parameters are present:

- provision AdminUser when `TargetAppUserId` and `Reason` are present;
- assign then revoke when `TargetAdminUserId`, `RoleId`, and `Reason` are present;
- disable then enable when `TargetAdminUserId` and `Reason` are present.

Do not run the mutation-enabled smoke casually against production. If a non-local or production-looking URL is intentionally used, `-AllowProductionUrl` is required as an additional explicit acknowledgement.

## Endpoints intentionally used by the optional smoke script

- `POST /api/auth/login`
- `GET /api/admin/role-assignments/actor`
- `GET /api/admin/role-assignments/diagnostics`
- `POST /api/admin/role-assignments/provision-admin-user`
- `POST /api/admin/role-assignments/assign`
- `POST /api/admin/role-assignments/revoke`
- `POST /api/admin/role-assignments/disable-admin`
- `POST /api/admin/role-assignments/enable-admin`

The smoke script must not call `POST /api/admin/role-assignments/bootstrap-first-owner`, billing endpoints, Premium grant/revoke endpoints, free-lesson reset endpoints, CMS write/publish/restore endpoints, or Paddle endpoints.

## Rollback and remediation notes

- Role-management mutations are audited; review audit events before and after remediation.
- Prefer correcting mistakes through audited revoke, disable, or enable actions where possible.
- If a role was assigned accidentally, revoke the role with a clear reason.
- If an admin user was enabled or should not remain active, disable the admin user with a clear reason.
- Database/manual remediation is a last resort, must be owner-approved, and must preserve enough audit context to explain what happened.
- Do not delete audit records as part of normal remediation.

## Known limitations

- Admin UI role management is an MVP for controlled tester/admin validation.
- BootstrapAdmin fallback still exists.
- Production Admin RBAC is not complete.
- Remaining endpoint migration is still pending.
- Owner-approved BootstrapAdmin fallback narrowing/removal is still pending.
- Final rollback procedures for public production operations are still pending.
