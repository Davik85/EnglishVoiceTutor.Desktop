# Admin RBAC Cutover Runbook

This runbook records the completed owner-approved controlled validation and permanent fallback disable. It is for owner-approved controlled validation only; it is not a broad public-production release checklist and must not be used to change BootstrapAdmin fallback casually.

## Current production state

The controlled Production Admin RBAC cutover rehearsal was completed successfully on 2026-06-22. It was followed by a separate permanent production fallback disable on 2026-06-22. The active production backend during the rehearsal was `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`; `/health` and `/api/health/database` returned `200 OK`, and the production database was healthy.

Persistent Admin RBAC state is stronger than before the rehearsal. A second backup `super_admin` account was created through the existing Admin Role Management UI. Final diagnostics after backup admin setup reported `totalAdminUsers=2`, `activeAdminUsers=2`, `activeRoleAssignments=2`, and `rolesInUse=super_admin`. Both approved admin accounts could log in to `/admin`. Both approved accounts passed `tools/smoke_admin_rbac_cutover_validation.ps1` while fallback was enabled.

Pre-rehearsal validation passed for both approved `super_admin` accounts against `https://api.languagevoicetutor.com` with `ExpectedFallbackEnabled true` and `ExpectedActorMappingFound true`. AdminPermission read endpoints and role-management read endpoints returned `200`. RBAC status showed `fallbackEnabled=True`, `defaultFallbackEnabled=True`, `configValuePresent=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

During the controlled rehearsal, a timestamped backup of `/etc/languagevoicetutor/backend.env` was created, `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies` was temporarily set to `false`, and `languagevoicetutor-backend.service` was restarted. Both approved `super_admin` accounts passed validation while fallback was disabled, with `ExpectedFallbackEnabled false` and `ExpectedActorMappingFound true`. AdminPermission read endpoints and role-management read endpoints returned `200`. RBAC status showed `fallbackEnabled=False`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Rollback/restoration also passed. `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies` was set back to `true`, `languagevoicetutor-backend.service` was restarted, and final validation passed with `ExpectedFallbackEnabled true`. Final RBAC status showed `fallbackEnabled=True`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Current final state: permanent BootstrapAdmin fallback disable has been completed. Production `backend.env` now sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`, `languagevoicetutor-backend.service` was restarted successfully, `/health` and `/api/health/database` returned `200 OK`, and both approved persistent `super_admin` accounts passed validation with `ExpectedFallbackEnabled false`, `ExpectedActorMappingFound true`, `ExpectedAdminPermissionEndpointStatus 200`, and `ExpectedRoleManagementEndpointStatus 200`. Current RBAC status showed `fallbackEnabled=False`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`. Rollback remains available by setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=true` and restarting the backend.

The release gate runs the Admin RBAC cutover validation static pack, which verifies cutover guardrails statically but does not perform live cutover. The manual cutover smoke script remains opt-in and outside the release gate. The setting is:

`AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies`

If the setting is missing or set to `true`, fallback is enabled; fallback remains enabled by default. If the setting is set to `false`, fallback is disabled only for `AdminPermission:*` policies.

## What fallback enabled means

- BootstrapAdmin users may still authorize `AdminPermission:*` endpoints through the temporary fallback path.
- Persistent-role users may authorize `AdminPermission:*` endpoints when their active role grants the required permission.
- This mode is appropriate for controlled validation before owner-approved cutover.

## What fallback disabled means

- `AdminPermission:*` endpoints require active persistent Admin role assignments with the matching permission.
- BootstrapAdmin fallback no longer authorizes `AdminPermission:*` endpoints.
- Role-assignment management endpoints protected by `AdminRoleManagementPermissionPolicyName` remain separate from this fallback switch.
- BootstrapAdmin-only endpoints, including CMS import/init endpoints, remain intentionally separate and are not controlled by this switch.

## Completed rehearsal pattern and rollback plan

The 2026-06-22 rehearsal completed this pattern successfully, and the later 2026-06-22 permanent disable also passed. Use the same safeguards for any future owner-approved fallback change. Complete all of the following before changing the fallback switch again:

1. The release gate passes, including the Admin RBAC cutover validation static pack.
2. Production health is green (`/health`, `/api/health/database`, and Admin endpoints).
3. Two persistent approved `super_admin` accounts exist; this is completed as of 2026-06-22.
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

- Do not change BootstrapAdmin fallback casually. Fallback is currently disabled explicitly in production, and re-enabling or disabling it again requires owner-approved controlled validation, rollback readiness, an owner-approved configuration change, and backend reload/restart.
- Do not run mutation smoke scripts against production casually.
- Do not expose credentials.
- Do not paste tokens, cookies, passwords, raw claims, raw response bodies, connection strings, certificates, provider payloads, or `SafeMetadataJson` into logs.
- Do not call Premium grant/revoke, free-lesson reset, billing cancel-renewal, CMS write/draft/save/import/init/validate/preview/publish/restore, role-management mutation, bootstrap-first-owner, Paddle, or billing provider endpoints during cutover validation.
