# Production Admin RBAC Readiness

Review date: 2026-06-22.

Scope: planning/documentation only. This document audits the current Admin / BootstrapAdmin / authorization state and defines the minimum production Admin RBAC target before support, content, billing, or auditor workflows are exposed broadly. It does not implement runtime RBAC, endpoint behavior changes, database schema changes, EF migrations, Admin UI behavior changes, billing/Paddle changes, entitlement changes, Desktop changes, secrets, or test credentials.

## 2026-06-22 production status update

Production Admin RBAC is advanced, and the controlled fallback cutover rehearsal has been completed successfully. Backend `0.1.35-backend.34` was the active production backend at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.34` during the rehearsal. `/health` and `/api/health/database` returned `200 OK`, and the production database was healthy.

Completed as of this update:

- AdminPermission endpoint migration foundation is complete for the currently migrated scope.
- 35 existing Admin endpoint registrations are protected by `AdminPermission:*` policies.
- The RBAC cutover status endpoint exists.
- The Admin UI displays cutover status read-only and does not provide a fallback-disable toggle.
- The Admin RBAC cutover validation static pack is part of the release gate.
- Production DB has Admin RBAC persistence tables: `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.
- Two persistent approved `super_admin` accounts now exist, including a second backup account created through the existing Admin Role Management UI.
- Both approved admin accounts can log in to `/admin`.
- Final role diagnostics after backup admin setup reported `totalAdminUsers=2`, `activeAdminUsers=2`, `activeRoleAssignments=2`, and `rolesInUse=super_admin`.
- Pre-rehearsal validation passed for both approved accounts with `ExpectedFallbackEnabled true` and `ExpectedActorMappingFound true`. AdminPermission read endpoints and role-management read endpoints returned `200`. Status showed `fallbackEnabled=True`, `defaultFallbackEnabled=True`, `configValuePresent=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.
- Controlled disabled-fallback rehearsal passed after a timestamped backup of `/etc/languagevoicetutor/backend.env`, temporarily setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`, and restarting `languagevoicetutor-backend.service`. Both approved accounts passed with `ExpectedFallbackEnabled false` and `ExpectedActorMappingFound true`. AdminPermission read endpoints and role-management read endpoints returned `200`. Status showed `fallbackEnabled=False`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.
- Rollback/restoration passed after setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=true` and restarting `languagevoicetutor-backend.service`. Final validation passed with `ExpectedFallbackEnabled true`. Final status showed `fallbackEnabled=True`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Important final state:

- This was a successful rehearsal, not permanent fallback removal.
- Production fallback is currently enabled explicitly through `backend.env`.
- Production Admin RBAC is stronger than before because two persistent `super_admin` accounts are verified and persistent-role authorization was proven to work with fallback disabled.
- The remaining owner decision is whether to keep BootstrapAdmin fallback enabled as a documented temporary exception or schedule a separate owner-approved permanent fallback-disable window.

Still pending before Production Admin RBAC can be accepted for public RC:

- Owner decision: documented temporary fallback exception or separately scheduled permanent fallback-disable window.
- Validation that non-owner roles behave correctly.
- Critical-change approval remains future work.

Public release is still not complete. Do not claim broad public-production readiness from this rehearsal.

## Executive status

The current Admin foundation is acceptable for controlled tester/direct Windows operations where a very small trusted operator set is configured through BootstrapAdmin and audit logs are reviewed. It is not suitable as the final public-production Admin model until the owner accepts either a documented temporary fallback exception or a permanent fallback-disable window, non-owner role validation is accepted, and critical-change approval is addressed. BootstrapAdmin fallback is currently enabled explicitly for production, and UI hiding or UI awareness alone must never be treated as authorization.

Public release candidate readiness requires production Admin RBAC with endpoint-level permission enforcement and an owner decision to permanently disable fallback, or a documented owner-approved exception that explicitly accepts the BootstrapAdmin risk for a narrow time window. UI hiding or UI awareness alone must never be treated as authorization.

Current implementation note: permission policy constants, registered permission policies, persistent Admin role assignment tables, and role-management validation now exist. The migrated `AdminPermission:*` endpoints can be authorized by persistent roles, as proven during the 2026-06-22 disabled-fallback rehearsal. Production RBAC endpoint-level enforcement is still not fully complete for every sensitive Admin action; a public release candidate still requires completion of the remaining endpoint decisions or an owner-approved exception.

Current static catalog update: `AdminRolePermissionCatalogService` contains the production role-to-permission catalog for Owner/Super Admin, Support, Content Editor, Billing Support, and Read-only Auditor. Production role assignment persistence now exists, the Admin UI role-management MVP has been used to create a second backup `super_admin`, and persistent-role authorization was proven with fallback disabled for the migrated `AdminPermission:*` endpoints. BootstrapAdmin fallback remains enabled explicitly after the rehearsal, and a public release candidate still requires the owner fallback decision plus remaining endpoint and non-owner role validation. Manual Premium grant/revoke remains Super Admin only in the static catalog; Billing Support receives billing diagnostics and cancel-renewal only.

Current endpoint/action catalog update: `AdminEndpointPermissionCatalog` now contains a static foundation mapping from current Admin endpoint/action identifiers to production admin permissions, plus documented future-only seams for permissions that do not yet have active endpoints. As a controlled proof of concept, exactly five safe read-only existing Admin endpoints now require matching permission policies: `GET /api/admin/me` (`admin.identity.read`, `AdminPermission:admin.self.read`), `GET /api/admin/capabilities` (`admin.capabilities.read`, `AdminPermission:admin.capabilities.read`), `GET /api/admin/statistics/overview` (`admin.product_overview.read`, `AdminPermission:product_statistics.read`), `GET /api/admin/dev/cms/runtime-status` (`admin.cms.runtime_status.read`, `AdminPermission:cms.runtime_status.read`), and one additional safe read-only CMS content endpoint, `GET /api/admin/dev/cms/content-packs` (`admin.cms.content_packs.list`, `AdminPermission:cms.content.read`). BootstrapAdmin/SuperAdmin access remains preserved through the permission handler and BootstrapAdmin permission catalog. This does not mean full production RBAC is enabled: role assignment persistence exists as a foundation but production Admin RBAC remains incomplete until endpoint migration is completed, operational Admin UI role management exists, BootstrapAdmin fallback is narrowed or removed by owner-approved cutover, and rollback procedures are finalized. Admin UI role management now exists as an MVP and has an operational validation runbook at `docs/ADMIN_ROLE_MANAGEMENT_UI_RUNBOOK.md`; the release gate now runs `tools/test_admin_ui_role_management_policy.py`, and an opt-in manual smoke script exists at `tools/smoke_admin_role_management_flow.ps1`. `bootstrap-first-owner` remains separate/manual and is not exposed in Admin UI. No write/user/billing/Premium/free-lesson endpoints were migrated; no CMS write/draft/publish/restore/import/init/validate/preview endpoints were migrated; and dangerous/write/user/billing/CMS write/Premium/free-lesson endpoints remain on `BootstrapAdmin` until migrated deliberately. Production Admin RBAC remains incomplete until operational validation, remaining endpoint migration, owner-approved BootstrapAdmin fallback narrowing/removal, and rollback procedures are finalized. An Admin RBAC cutover runbook now exists at `docs/ADMIN_RBAC_CUTOVER_RUNBOOK.md`, and the manual opt-in validation smoke script now exists at `tools/smoke_admin_rbac_cutover_validation.ps1`; the Admin RBAC cutover validation static pack is now part of the release gate. The release gate verifies cutover guardrails statically but does not perform live cutover, and the cutover smoke script remains manual and opt-in. Fallback is currently enabled explicitly through production `backend.env`, and disabling it again requires owner-approved controlled validation with rollback readiness.


## Current Admin / BootstrapAdmin / authorization audit

### How admin access is currently created or bootstrapped

- Admin access is configured through `AdminBootstrap` options with `Enabled` and an `AdminEmails` allow-list.
- `BootstrapAdminAccessService` checks the authenticated user's email against the configured BootstrapAdmin email list.
- `BootstrapAdminAuthorizationHandler` satisfies the `BootstrapAdmin` policy when `BootstrapAdminAccessService.IsBootstrapAdmin(...)` returns true.
- The `BootstrapAdmin` authorization policy accepts the normal JWT bearer scheme and the admin shell cookie scheme, requires an authenticated user, and adds the BootstrapAdmin requirement.
- `/api/admin/me` reports `adminSource=development_config_bootstrap`, `isBootstrapAdmin=true`, BootstrapAdmin roles, and BootstrapAdmin permissions for an authorized BootstrapAdmin.
- Bootstrap admins map to `super_admin` and currently receive the full admin permission set from `AdminRolePermissionCatalogService`.
- Production role assignment persistence exists, and role management is available only for controlled admin validation; it is not a broad public-production admin model yet.

### Current role and permission foundation

Existing role constants are foundation names only and are not production role assignments yet:

- `super_admin`
- `support`
- `content_editor`
- `billing_support`
- `read_only_auditor`

Existing permission constants cover admin self/capabilities, users, audit, CMS, runtime status, subscriptions diagnostics, premium grant/revoke, free lesson allowance reset, billing diagnostics, and product statistics. A technical seam now also defines and registers explicit production permission policy names for the documented least-privilege Admin RBAC actions, including user lookup/overview, lesson history diagnostics, premium diagnostics, cancel-renewal, system diagnostics, and admin role management. These policies are foundation-only and BootstrapAdmin-compatible: endpoint authorization still uses the broad BootstrapAdmin policy instead of per-permission policies, so this step does not change runtime admin access behavior.

### Admin endpoints and admin-like endpoints currently present

Current protected Admin endpoints include:

- Admin identity/capabilities: `GET /api/admin/me`, `GET /api/admin/capabilities`, `DELETE /api/admin/session`.
- Product/system overview: `GET /api/admin/statistics/overview`.
- User lookup/overview/diagnostics: lookup by email, lookup by user id, and target-user audit action history.
- Entitlement/support actions: manual Premium grant, manual Premium revoke, free lesson allowance reset.
- Billing support action: admin cancel-renewal for a target user.
- Development/Admin CMS operations: static content import/initialize, published content status, runtime content status, content pack/topic/scenario/prompt template/tutor behavior profile read/update, CMS audit entries, validation, preview summary, versions, publish, and restore.

Admin-like non-Admin routes also exist for learner billing/subscription flows and provider webhooks. They remain backend-owned and must not be called directly by Desktop or Admin UI except through documented backend endpoints. This RBAC plan does not change those flows.

### Which actions are currently protected and how

Protected by backend `BootstrapAdmin` policy:

- Admin identity/capabilities reads.
- Product statistics overview.
- User lookup and user-level diagnostics returned by admin lookup responses.
- Target-user admin audit action reads.
- Manual Premium grant/revoke.
- Free lesson allowance reset.
- Admin cancel-renewal.
- CMS development/admin reads, draft saves, publish, restore, import/initialize, audit list, validation, preview, and runtime status diagnostics.

Protection type today:

- Backend policy-based protection exists, but it is one broad `BootstrapAdmin` policy.
- BootstrapAdmin membership is effectively claim/user-email based because it compares the authenticated user's email to configured `AdminBootstrap:AdminEmails`.
- Existing roles/permissions are constants and response metadata; they are not currently enforced endpoint-by-endpoint.
- The Admin Shell is UI-aware only and must not be treated as a security boundary.

### Dangerous or sensitive actions currently present or likely to become present

Dangerous/sensitive actions already present:

- Manual Premium grant.
- Manual Premium revoke.
- Free lesson allowance reset.
- Admin cancel-renewal.
- CMS publish.
- CMS restore/rollback.
- CMS static import/initialize if available outside a tightly controlled development/operator context.
- User lookup and user-level diagnostics, because they can expose account, subscription, entitlement, usage, lesson history, settings, and support-relevant data.
- Billing event diagnostics, where available, because even safe provider metadata can be sensitive in aggregate.
- Audit log reads, because audit data includes admin ids, target user ids, reasons, timestamps, action types, and safe metadata.
- Future role/permission changes.

Dangerous/sensitive actions likely to become present as production Admin matures:

- Admin role assignment, role removal, permission grants, permission revocation, and emergency owner recovery.
- Critical CMS approval workflows.
- Refund/chargeback/customer portal support actions if ever added.
- Account disable/delete/export/support recovery actions if ever added.

### Current audit logging that exists

Target-user admin action audit logging exists for:

- Manual Premium grant.
- Manual Premium revoke.
- Free lesson allowance reset.
- Admin cancel-renewal.

CMS content audit logging exists for:

- Static import-created/import-updated/import-published paths.
- Draft saves for topics, scenarios, prompt templates, and tutor behavior profiles.
- CMS publish.
- CMS restore/rollback as new version behavior.

Audit records generally include actor id, target user or target content entity, action, reason or summary where applicable, timestamp, status/result for CMS audit entries, and safe metadata. Existing user-action audit is target-user oriented and does not by itself provide a general access-audit stream for read-only viewing.

### Audit logging gaps or incomplete areas

- User lookup, user overview, lesson history diagnostics, premium diagnostics, billing diagnostics, product statistics, system/runtime diagnostics, and audit-log viewing are not fully documented as access-audited reads. If the current audit model supports access auditing later, highly sensitive user-level diagnostic views should write access audit records.
- Production role/permission changes do not exist yet and therefore do not have implemented audit records.
- The target-user audit table records support actions, but it is not a complete production security audit/event stream for all admin reads and role-management events.
- The Admin Shell permission display is not authorization and does not produce evidence that backend endpoint-level enforcement occurred.
- CMS audit listing currently focuses on content audit entries; production audit review should define retention, filtering, export, and access controls before broader use.

### Controlled-testing behavior that is acceptable now

Acceptable for controlled tester/direct Windows operations only:

- A small, trusted BootstrapAdmin allow-list.
- BootstrapAdmin mapping to `super_admin` and the full permission set.
- UI-awareness role/permission display that does not block tabs, buttons, or backend calls.
- Manual Premium grant/revoke, free lesson reset, admin cancel-renewal, CMS draft save/publish/restore, and CMS runtime diagnostics when performed by trusted operators with reason fields and audit review.
- Production billing/live Paddle operations remaining deferred while sandbox checkout/cancel-renewal validation continues.

### Behavior not suitable for public production

Not suitable as the broad public production Admin model:

- All BootstrapAdmins receiving full `super_admin` permissions.
- No endpoint-level per-role/per-permission enforcement.
- No production admin role assignment/revocation workflow.
- No strict separation between support, content, billing, auditor, and owner responsibilities.
- Support or content operators being technically able to call billing/entitlement endpoints if they have any admin session.
- Billing support being technically able to edit or publish CMS content.
- Read-only auditors being technically able to call write endpoints.
- No implemented production audit trail for role/permission changes.
- No documented owner approval/exception if BootstrapAdmin remains active for a public release candidate.

## Target minimal production role model

### Owner / Super Admin

Allowed actions:

- View admin self/capabilities.
- View system diagnostics, product statistics, audit logs, user lookup, user overview, lesson history diagnostics, premium diagnostics, and billing event diagnostics.
- Perform support actions: manual Premium grant/revoke, free lesson reset, and cancel-renewal.
- Perform CMS draft saves, CMS publish, and CMS restore/rollback.
- Manage production admin roles and permissions.

Forbidden actions:

- Directly editing provider data, secrets, raw Paddle payloads, or database rows through Admin UI unless a separate approved operations process exists.
- Bypassing backend-owned billing/entitlement rules.
- Any action without required reason/summary where the endpoint requires it.

Dangerous actions requiring stricter permissions:

- Role/permission changes, manual Premium grant/revoke, free lesson reset, cancel-renewal, CMS publish, CMS restore/rollback, and any future account destructive action.

Sensitive user-level data:

- May view sensitive user-level diagnostics when needed for operations and audit review.

Write actions:

- May perform write actions, subject to endpoint permission enforcement, reason/summary requirements, audit logging, and future critical-change approval where applicable.

Required audit logging:

- Mandatory for every dangerous/sensitive write, role/permission change, and highly sensitive diagnostic read if access auditing is implemented.

### Support

Allowed actions:

- View admin self/capabilities.
- Perform user lookup and user overview for support cases.
- View lesson history diagnostics and premium diagnostics needed to troubleshoot access issues.
- View target-user support audit history where needed.
- Reset free lesson allowance only if explicitly granted as a support recovery action.

Forbidden actions:

- CMS draft save, CMS publish, CMS restore/rollback.
- Manual Premium grant/revoke unless separately elevated; the minimal target model forbids it for normal Support.
- Cancel-renewal unless separately elevated; the minimal target model forbids it for normal Support.
- Billing event diagnostics unless separately elevated.
- Admin role management.
- System-wide sensitive diagnostics beyond support needs.

Dangerous actions requiring stricter permissions:

- Free lesson reset, any entitlement write, any billing write, and any account destructive action.

Sensitive user-level data:

- May view limited sensitive user-level support data necessary for a support case. Should not see raw secrets, tokens, raw provider payloads, full provider ids, or unnecessary lesson content.

Write actions:

- Minimal model allows only narrowly scoped support write actions such as free lesson reset when explicitly granted and audited.

Required audit logging:

- Mandatory for free lesson reset and any future support write; access auditing should be considered for highly sensitive diagnostics.

### Content Editor

Allowed actions:

- View admin self/capabilities.
- View CMS content packs, topics, scenarios, prompt templates, tutor behavior profiles, validation, preview summary, versions, and CMS content audit entries.
- Save CMS drafts.
- Publish CMS changes only if the Content Editor role is explicitly granted publish permission; otherwise publish should require Owner/Super Admin or an approval workflow.

Forbidden actions:

- User lookup, user overview, lesson history diagnostics, premium diagnostics, manual Premium grant/revoke, free lesson reset, cancel-renewal, billing event diagnostics, admin role management, and broad system diagnostics unrelated to CMS.

Dangerous actions requiring stricter permissions:

- CMS publish and CMS restore/rollback. These affect learner runtime content and should require stricter permission, reason/summary, audit, and later critical-change approval.

Sensitive user-level data:

- No user-level support/billing data access by default.

Write actions:

- May save CMS drafts. May publish or restore only if explicitly granted stricter content-production permissions.

Required audit logging:

- Mandatory for draft saves, publish, restore/rollback, import/initialize, and any future critical content change.

### Billing Support

Allowed actions:

- View admin self/capabilities.
- Perform user lookup/user overview limited to billing support needs.
- View premium diagnostics and billing event diagnostics.
- Perform cancel-renewal when required for a support case.
- View target-user support audit history where needed.

Forbidden actions:

- CMS draft save, CMS publish, CMS restore/rollback.
- Manual Premium grant/revoke unless separately elevated; the minimal target model should keep entitlement writes stricter than ordinary billing diagnostics.
- Free lesson reset unless separately granted.
- Admin role management.
- Raw provider payload, secrets, API keys, webhook secrets, full provider ids, or direct Paddle calls from Admin UI.

Dangerous actions requiring stricter permissions:

- Cancel-renewal, manual Premium grant/revoke, refunds/chargebacks/customer portal actions if later added, and any direct entitlement modification.

Sensitive user-level data:

- May view billing-relevant sensitive user-level data and safe provider diagnostics only. Must not view raw provider payloads or secrets through broad Admin views.

Write actions:

- May perform cancel-renewal with reason and audit. Other entitlement or billing writes require stricter explicit permission.

Required audit logging:

- Mandatory for cancel-renewal and any future billing/entitlement write. Access auditing should be considered for highly sensitive billing diagnostics.

### Read-only Auditor

Allowed actions:

- View admin self/capabilities.
- View audit logs.
- View system diagnostics and product statistics when needed for compliance/operations review.
- View user-level diagnostics only if explicitly approved and minimized for audit purpose.

Forbidden actions:

- All write actions: CMS draft save, CMS publish, CMS restore/rollback, manual Premium grant/revoke, free lesson reset, cancel-renewal, admin role management, imports/initialization, and account/billing/content mutation.

Dangerous actions requiring stricter permissions:

- Any write action is forbidden for this role and requires role change/elevation outside the read-only auditor role.

Sensitive user-level data:

- Default should be minimized. May view sensitive diagnostics only when required for audit and allowed by endpoint permission.

Write actions:

- None.

Required audit logging:

- Audit log viewing itself should be access-audited if the audit model supports read access auditing, especially when viewing target-user or sensitive billing/support records.

## Permission matrix

Legend: `Allow` means the role may receive the permission in the minimal production target. `Forbid` means the role must not receive it by default. `Strict` means the action is dangerous and should require stricter permission, reason/summary, audit logging, and possibly later approval/elevation.

| Permission / action | Owner / Super Admin | Support | Content Editor | Billing Support | Read-only Auditor |
| --- | --- | --- | --- | --- | --- |
| CMS draft/save | Allow | Forbid | Allow | Forbid | Forbid |
| CMS publish | Strict allow | Forbid | Strict allow only if explicitly granted | Forbid | Forbid |
| CMS restore/rollback | Strict allow | Forbid | Strict allow only if explicitly granted | Forbid | Forbid |
| User lookup | Allow | Allow | Forbid | Allow | Forbid by default; allow only if explicitly approved |
| User overview | Allow | Allow | Forbid | Allow limited billing/support view | Forbid by default; allow only if explicitly approved |
| Lesson history diagnostics | Allow | Allow limited support view | Forbid | Forbid by default | Forbid by default; allow only if explicitly approved |
| Premium diagnostics | Allow | Allow | Forbid | Allow | Forbid by default; allow only if explicitly approved |
| Manual Premium grant | Strict allow | Forbid | Forbid | Forbid by default; strict only if separately elevated | Forbid |
| Manual Premium revoke | Strict allow | Forbid | Forbid | Forbid by default; strict only if separately elevated | Forbid |
| Free lesson reset | Strict allow | Strict allow if explicitly granted | Forbid | Forbid by default; allow only if separately granted | Forbid |
| Cancel-renewal | Strict allow | Forbid by default | Forbid | Strict allow | Forbid |
| Billing event diagnostics | Allow | Forbid by default | Forbid | Allow | Forbid by default; allow only if explicitly approved |
| Audit log view | Allow | Allow target-user support audit where needed | Allow CMS audit only | Allow billing/support audit where needed | Allow |
| System diagnostics | Allow | Forbid by default | CMS/runtime status only if needed | Billing status only if needed | Allow read-only |
| Admin role management | Strict allow | Forbid | Forbid | Forbid | Forbid |

## Endpoint-level authorization requirements

- Admin UI awareness is not enough.
- Backend endpoints must enforce permissions.
- Dangerous actions must be blocked server-side even if someone manually calls the API.
- Support and Content Editor roles must not be able to accidentally perform billing or entitlement actions.
- Billing Support must not be able to edit content or publish CMS changes unless explicitly granted.
- Read-only Auditor must not be able to perform write actions.
- Owner / Super Admin role management must be strictly limited.
- Each Admin endpoint should map to one explicit permission policy or a small, reviewed set of permission policies.
- Endpoint tests should prove forbidden roles receive `403 Forbidden` even when a request is otherwise authenticated.
- Authentication proves who the admin is; authorization must prove what the admin can do.
- Client-side tab/button hiding is a usability layer only and must never be the only control.

Recommended endpoint grouping for future implementation:

- `admin.self.read`: `/api/admin/me`.
- `admin.capabilities.read`: `/api/admin/capabilities`.
- `product_statistics.read` or `system.diagnostics.read`: product statistics and safe system diagnostics.
- `users.read`: user lookup and user overview.
- `users.diagnostics.read`: lesson history diagnostics and premium diagnostics.
- `audit.read`: target-user admin audit logs and CMS audit logs, with scoped variants if needed.
- `cms.content.read`: CMS content pack/topic/scenario/prompt/tutor/version reads, validation, preview.
- `cms.content.write_draft`: CMS draft saves and safe draft-only imports if retained.
- `cms.content.publish`: CMS publish.
- `cms.content.restore`: CMS restore/rollback.
- `subscriptions.diagnostics.read`: premium/subscription diagnostics.
- `premium.grant`: manual Premium grant.
- `premium.revoke`: manual Premium revoke.
- `free_lesson_allowance.reset`: free lesson reset.
- `billing.diagnostics.read`: billing event diagnostics.
- `billing.cancel_renewal`: admin cancel-renewal. This should be added as a distinct production permission rather than relying only on billing diagnostics.
- `admin.roles.manage`: role and permission management. This should be added as a distinct production permission before role management exists.

## Audit logging requirements

Every dangerous or sensitive action must include:

- actor admin user id;
- target user id or target entity id when applicable;
- action type;
- reason when applicable;
- timestamp;
- result/status;
- safe metadata only.

Dangerous/sensitive actions requiring mandatory audit logging:

- manual Premium grant;
- manual Premium revoke;
- free lesson reset;
- cancel-renewal;
- CMS publish;
- CMS restore/rollback;
- role/permission changes;
- viewing highly sensitive user-level diagnostics if the current/future audit model supports access auditing.

Safe metadata must not include secrets, Authorization headers, access tokens, refresh tokens, password reset codes, API keys, webhook secrets, connection strings, raw provider payloads, full provider ids, raw payment details, raw lesson audio, or unnecessary lesson message content.

Audit behavior must remain mandatory when production RBAC is introduced. RBAC must not weaken existing support-action or CMS audit records.

## Recommended first safe implementation slice after this planning step

Recommended first slice: add explicit production permission policy constants/tests while mapping existing BootstrapAdmin/SuperAdmin behavior to the current full-access behavior, without changing endpoint behavior for existing BootstrapAdmins.

Why this is safe:

- It can be implemented behind the existing BootstrapAdmin behavior so trusted controlled-test operators keep the same access.
- It creates named permission-policy seams and tests before any new production role assignments are enabled.
- It does not require EF migrations, database schema changes, Admin UI behavior changes, Desktop changes, billing/Paddle changes, or entitlement behavior changes.
- It lets the team add endpoint-by-endpoint authorization tests that initially prove parity for BootstrapAdmin and then gradually prove least-privilege denial for future roles.
- It reduces risk by separating policy vocabulary and test coverage from the later role storage/assignment implementation.

Alternative safe first slice: add read-only policy tests around one low-risk diagnostics endpoint, proving that a future read-only permission can access only that endpoint while writes remain denied. This should still avoid changing production access behavior until the owner approves a role rollout.

## Release-hardening checklist item

Before a public release candidate or broad Admin rollout:

- BootstrapAdmin is acceptable for controlled testing only.
- Public release candidate requires production Admin RBAC or a documented owner-approved exception.
- Endpoint-level permission enforcement is required before exposing support/content/billing admin actions broadly.
- Audit logging must remain mandatory for dangerous actions.
- Desktop must not call Paddle directly.
- Admin UI must not call Paddle directly.
- Backend remains the source of truth for billing, subscriptions, entitlements, trial, Premium, free access, and limits.

## Controlled CMS read-only endpoint batch migration update

The remaining eligible existing read-only Admin endpoints have been batch-migrated from `BootstrapAdminPolicyName` to narrow `AdminPermission:*` policies, and the CMS authoring workflow endpoints for draft save, validation, preview summary, publish, and version restore have now been batch-migrated as a controlled follow-up. There are now exactly 35 existing Admin endpoint registrations protected directly by AdminPermission policies: admin self-read, capabilities, product statistics overview, user lookup by email, user overview by id, user audit-log read, Premium grant/revoke, free lesson allowance reset, billing cancel-renewal, CMS runtime/status/content read endpoints, CMS audit-read endpoints, CMS version/content detail read endpoints, and CMS authoring workflow endpoints using `cms.content.write_draft`, `cms.content.read`, `cms.content.publish`, or `cms.content.restore` as appropriate. The latest controlled batch migrated `POST /api/admin/users/{userId:guid}/premium-grants` to `premium.grant`, `POST /api/admin/users/{userId:guid}/premium-grants/{entitlementId:guid}/revoke` to `premium.revoke`, `POST /api/admin/users/{userId:guid}/free-lesson-allowance/reset` to `free_lesson_allowance.reset`, and `POST /api/admin/users/{userId:guid}/billing/cancel-renewal` to `billing.cancel_renewal` without changing handler business logic.

BootstrapAdmin fallback remains preserved by `AdminPermissionAuthorizationHandler` for the controlled tester rollout, so BootstrapAdmin operators retain access while persistent-role admins must hold the required active permission. Role-assignment management endpoints remain on `AdminRoleManagementPermissionPolicyName`. CMS import/init endpoints remain BootstrapAdmin-protected. Premium grant/revoke, free-lesson reset, and billing cancel-renewal now use narrow AdminPermission policies while BootstrapAdmin fallback remains preserved; other still-unmigrated mutating or sensitive endpoints remain BootstrapAdmin-protected.

Admin UI Role Management MVP now exists for controlled operations using the existing guarded role-assignment endpoints, but it was not expanded by the CMS authoring workflow migration or the user-impacting Admin action endpoint migration. It does not expose first-owner bootstrap, does not create app users or invites, and does not remove BootstrapAdmin fallback. Production Admin RBAC remains incomplete until final operational validation, owner-approved BootstrapAdmin fallback narrowing/removal, and rollback procedures are finalized.

## Controlled BootstrapAdmin fallback cutover switch

`AdminPermission:*` policy evaluation now has a controlled BootstrapAdmin fallback cutover switch: `AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies`. The setting is enabled by default when it is missing, so the controlled tester rollout preserves the current mixed-mode behavior unless an owner explicitly changes configuration.

When the switch is enabled, persistent active Admin role assignments are evaluated first and the existing BootstrapAdmin fallback remains available for `AdminPermission:*` policies. When the switch is set to `false`, `AdminPermission:*` policies fail closed unless persistent active Admin role assignments grant the required permission through the production role-permission catalog.

This switch affects only `AdminPermission:*` policy fallback behavior. BootstrapAdmin-only endpoints, including CMS import/init endpoints, remain BootstrapAdmin-protected. Role-assignment management endpoints remain protected by `AdminRoleManagementPermissionPolicyName`.

Do not disable the fallback until persistent Owner/SuperAdmin mapping, actor resolution, diagnostics, and the expected critical role assignments have been validated in the target environment. Production Admin RBAC remains incomplete until operational validation, fallback cutover execution, rollback procedures, and final production checks are completed.


## Safe RBAC cutover status surface

A safe read-only backend status endpoint now exists at `GET /api/admin/rbac/cutover-status` behind `AdminRoleManagementPermissionPolicyName`. It reports only safe cutover fields, including the effective BootstrapAdmin fallback setting for `AdminPermission:*` policies, the default-enabled behavior, whether the configuration value is present, persistent role authorization status, mode text, and `generatedAtUtc`.

The Admin UI Role Management page displays this cutover status read-only. There is no Admin UI toggle or control to disable fallback. The cutover smoke validates `-ExpectedFallbackEnabled` against the backend-reported status when the parameter is provided.

Fallback remains enabled by default. Disabling fallback still requires an owner-approved config change to `AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies=false` and backend reload/restart through the documented operational process. Production Admin RBAC remains incomplete until controlled cutover and rollback validation are completed.
