# Admin RBAC Cutover Runbook

This runbook is for owner-approved controlled validation only. It is not a broad public-production release checklist and must not be used to disable BootstrapAdmin fallback casually.

## Current production state

First owner bootstrap has been completed in production. Actor mapping now resolves for the owner account, the active persistent role is `super_admin`, and the cutover status endpoint reports fallback enabled by default: `fallbackEnabled=true`, `defaultFallbackEnabled=true`, `configValuePresent=false`, and `persistentRoleAuthorizationEnabled=true`. Backend `0.1.35-backend.34` is deployed after production migration `20260620165657_AddAdminRoleAssignmentPersistence`, and `0.1.35-backend.33` remains available for rollback.

BootstrapAdmin fallback remains enabled by default for `AdminPermission:*` policies. The release gate runs the Admin RBAC cutover validation static pack, which verifies cutover guardrails statically but does not perform a live cutover. The manual cutover smoke script remains opt-in and outside the release gate. The setting is:

`AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies`

If the setting is missing or set to `true`, fallback is enabled. If the setting is set to `false`, fallback is disabled only for `AdminPermission:*` policies.

## What fallback enabled means

- BootstrapAdmin users may still authorize `AdminPermission:*` endpoints through the temporary fallback path.
- Persistent-role users may authorize `AdminPermission:*` endpoints when their active role grants the required permission.
- This mode is appropriate for controlled validation before owner-approved cutover.

## What fallback disabled means

- `AdminPermission:*` endpoints require active persistent Admin role assignments with the matching permission.
- BootstrapAdmin fallback no longer authorizes `AdminPermission:*` endpoints.
- Role-assignment management endpoints protected by `AdminRoleManagementPermissionPolicyName` remain separate from this fallback switch.
- BootstrapAdmin-only endpoints, including CMS import/init endpoints, remain intentionally separate and are not controlled by this switch.

## Next action: controlled cutover rehearsal plan

The next action is not to disable fallback immediately. The next action is to prepare and run a short owner-approved controlled cutover rehearsal with a documented rollback path. Complete all of the following before setting the fallback switch to `false`:

1. The release gate passes, including the Admin RBAC cutover validation static pack.
2. Production health is green (`/health`, `/api/health/database`, and Admin endpoints).
3. A persistent first Owner/SuperAdmin exists; this is already completed for the owner-equivalent account.
4. Actor mapping works for the operator account; this now resolves in production.
5. Role-assignment diagnostics are healthy and confirm the active `super_admin` role.
6. Required production Admin roles are assigned for the rehearsal scope.
7. Admin UI Role Management works for controlled validation.
8. `tools/smoke_admin_role_management_flow.ps1` has been validated in a safe environment.
9. `tools/smoke_admin_rbac_cutover_validation.ps1` passes in fallback-enabled mode first.
10. Prepare the exact placeholder-safe config change for disabling fallback, the exact placeholder-safe rollback command/config restoration for re-enabling fallback, and the backend restart/reload command.
11. Rollback owner, rollback timing, and backend restart/reload process are agreed before cutover.

Use placeholder accounts such as `<admin-email>` in notes. Do not paste real credentials, tokens, cookies, connection strings, certificates, or provider secrets into documents, chat, tickets, or logs.

## Cutover validation steps

1. Confirm owner approval for a controlled validation window.
2. Confirm the current backend deployment/service restart process from the project deployment documentation.
3. Confirm fallback-enabled validation passes:

   ```powershell
   tools/smoke_admin_rbac_cutover_validation.ps1 `
     -BaseUrl "<controlled-backend-url>" `
     -AdminEmail "<admin-email>" `
     -AdminPassword "<admin-password>" `
     -ExpectedFallbackEnabled true `
     -ExpectedActorMappingFound true `
     -ConfirmRbacCutoverValidation
   ```

4. During the owner-approved short cutover window only, set `AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies` to `false` using the pre-reviewed placeholder-safe production configuration procedure.
5. Restart or reload the backend using the project’s documented deployment/service process.
6. Run cutover validation smoke again with expected statuses for the account under test:

   ```powershell
   tools/smoke_admin_rbac_cutover_validation.ps1 `
     -BaseUrl "<controlled-backend-url>" `
     -AdminEmail "<admin-email>" `
     -AdminPassword "<admin-password>" `
     -ExpectedFallbackEnabled false `
     -ExpectedActorMappingFound true `
     -ExpectedAdminPermissionEndpointStatus 200 `
     -ExpectedRoleManagementEndpointStatus 200 `
     -ConfirmRbacCutoverValidation `
     -AllowProductionUrl
   ```

7. Verify AdminPermission endpoints still work through persistent roles. Test representative `AdminPermission:*` read endpoints through the smoke script:
   - `GET /api/admin/me`
   - `GET /api/admin/capabilities`
   - `GET /api/admin/statistics/overview`
   - `GET /api/admin/dev/cms/runtime-status`
8. Test role-management read endpoints through the smoke script:
   - `GET /api/admin/role-assignments/actor`
   - `GET /api/admin/role-assignments/diagnostics`
   - `GET /api/admin/rbac/cutover-status` (safe read-only status: effective fallback enabled, default enabled, config key, config-value-present summary, persistent role authorization enabled, and generated timestamp)
9. Confirm the smoke script compares `-ExpectedFallbackEnabled` with the backend-reported `GET /api/admin/rbac/cutover-status` value when that parameter is provided. The script still does not change backend configuration.
10. Confirm the Admin UI Role Management page displays the RBAC cutover status read-only and does not expose a fallback toggle or disable control.
11. Confirm BootstrapAdmin-only CMS import/init endpoints are still intentionally separate and remain BootstrapAdmin-protected. Do not call CMS import/init as part of this cutover smoke.
12. Record only safe status summaries and owner approval metadata. Do not record raw response bodies, raw claims, tokens, cookies, passwords, `SafeMetadataJson`, or secrets.

## Rollback

If validation fails, if any unexpected issue appears, or if the owner cancels cutover:

1. Set `AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies` back to `true`, or remove the setting so the default enabled behavior applies.
2. Restart or reload the backend using the project’s documented deployment/service process.
3. Rerun `tools/smoke_admin_rbac_cutover_validation.ps1` in fallback-enabled mode.
4. Verify BootstrapAdmin fallback behavior is restored for `AdminPermission:*` endpoints.
5. Keep role-management diagnostics and Admin UI validation available for investigation, but do not run mutation smoke scripts against production casually.

## Warnings

- Do not disable BootstrapAdmin fallback casually. Fallback remains enabled by default, and disabling it still requires owner-approved controlled validation, rollback readiness, an owner-approved configuration change, and backend reload/restart.
- Do not run mutation smoke scripts against production casually.
- Do not expose credentials.
- Do not paste tokens, cookies, passwords, raw claims, raw response bodies, connection strings, certificates, provider payloads, or `SafeMetadataJson` into logs.
- Do not call Premium grant/revoke, free-lesson reset, billing cancel-renewal, CMS write/draft/save/import/init/validate/preview/publish/restore, role-management mutation, bootstrap-first-owner, Paddle, or billing provider endpoints during cutover validation.
