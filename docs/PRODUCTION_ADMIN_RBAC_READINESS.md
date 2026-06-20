# Production Admin RBAC Readiness

Review date: 2026-06-20.

Scope: planning/documentation only. This document audits the current Admin / BootstrapAdmin / authorization state and defines the minimum production Admin RBAC target before support, content, billing, or auditor workflows are exposed broadly. It does not implement runtime RBAC, endpoint behavior changes, database schema changes, EF migrations, Admin UI behavior changes, billing/Paddle changes, entitlement changes, Desktop changes, secrets, or test credentials.

## Executive status

The current Admin foundation is acceptable for controlled tester/direct Windows operations where a very small trusted operator set is configured through BootstrapAdmin and audit logs are reviewed. It is not suitable as the final public-production Admin model because BootstrapAdmin maps every configured admin email to the full `super_admin` permission set, the Admin Shell roles/permissions display is UI-awareness only, and backend endpoints do not yet enforce separate least-privilege permissions per role.

Public release candidate readiness requires production Admin RBAC with endpoint-level permission enforcement, or a documented owner-approved exception that explicitly accepts the BootstrapAdmin risk for a narrow time window. UI hiding or UI awareness alone must never be treated as authorization.

Current implementation note: the permission policy constants and registered permission policies are now available as a foundation seam and do not change current runtime admin access behavior. Existing Admin endpoints still use the broad BootstrapAdmin policy and have not been switched to the new permission policies. Production RBAC endpoint-level enforcement is still not fully enabled; a public release candidate still requires endpoint-level permission enforcement for these policies or an owner-approved exception.

Current static catalog update: `AdminRolePermissionCatalogService` now contains a foundation-only production role-to-permission catalog for Owner/Super Admin, Support, Content Editor, Billing Support, and Read-only Auditor. The catalog is intentionally static and is not role assignment persistence. There are still no production role tables, EF migrations, Admin UI role-management screens, or endpoint-level production RBAC enforcement. BootstrapAdmin remains controlled-testing only, and a public release candidate still requires endpoint-level permission enforcement or an explicit owner-approved exception. Manual Premium grant/revoke remains Super Admin only in the static catalog; Billing Support receives billing diagnostics and cancel-renewal only.

Current endpoint/action catalog update: `AdminEndpointPermissionCatalog` now contains a static foundation mapping from current Admin endpoint/action identifiers to production admin permissions, plus documented future-only seams for permissions that do not yet have active endpoints. As a proof of concept, exactly one safest read-only endpoint, `GET /api/admin/me` (`admin.identity.read`), now requires the matching `AdminPermission:admin.self.read` policy. BootstrapAdmin/SuperAdmin access remains preserved through the permission handler and BootstrapAdmin permission catalog. This does not mean full production RBAC is enabled: role assignment persistence still does not exist, Admin UI role management still does not exist, and dangerous/write/billing/CMS/Premium/free-lesson endpoints remain on `BootstrapAdmin` until migrated deliberately. A public release candidate still requires full endpoint-level permission enforcement or an owner-approved exception.


## Current Admin / BootstrapAdmin / authorization audit

### How admin access is currently created or bootstrapped

- Admin access is configured through `AdminBootstrap` options with `Enabled` and an `AdminEmails` allow-list.
- `BootstrapAdminAccessService` checks the authenticated user's email against the configured BootstrapAdmin email list.
- `BootstrapAdminAuthorizationHandler` satisfies the `BootstrapAdmin` policy when `BootstrapAdminAccessService.IsBootstrapAdmin(...)` returns true.
- The `BootstrapAdmin` authorization policy accepts the normal JWT bearer scheme and the admin shell cookie scheme, requires an authenticated user, and adds the BootstrapAdmin requirement.
- `/api/admin/me` reports `adminSource=development_config_bootstrap`, `isBootstrapAdmin=true`, BootstrapAdmin roles, and BootstrapAdmin permissions for an authorized BootstrapAdmin.
- Bootstrap admins map to `super_admin` and currently receive the full admin permission set from `AdminRolePermissionCatalogService`.
- `ProductionRolesAvailable` remains false in the capabilities response; production role management is not enabled.

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
