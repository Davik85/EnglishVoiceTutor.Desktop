# Admin Role Assignment Persistence Plan

Review date: 2026-06-21.

Scope: planning/model design and staged foundation tracking. EF entities/migration, read-only role assignment query service, internal write service, guarded assign/revoke/disable-admin endpoints, controlled first-owner bootstrap endpoint, and trusted actor-resolution seam now exist, but this document does not activate runtime authorization from persistent roles, Admin UI role management, create-admin/invite endpoints, additional existing endpoint policy migrations, billing/Paddle changes, entitlement changes, Desktop changes, packaging changes, generated artifacts, or secrets.

A read-only persistent role assignment service (`IAdminRoleAssignmentReadService` / `AdminRoleAssignmentReadService`) now exists to load active, non-revoked roles for active, non-disabled persistent admin users from the new EF tables. A trusted actor resolver (`IAdminRoleAssignmentActorResolver` / `AdminRoleAssignmentActorResolver`) now exists for role assignment workflows; it derives actor mapping from server-side authenticated claims and read-only persistent role-assignment reads, and it does not trust actor admin ids or actor role ids from request bodies. A read-only current-actor mapping diagnostics endpoint (`GET /api/admin/role-assignments/actor`) now uses that trusted resolver to report whether the authenticated admin maps to a persistent `AdminUser` and actor role ids; it does not expose email, raw claims, tokens, raw metadata, or provider payloads, does not mutate role assignment state, and does not activate persistent roles globally. A validation-only role assignment safety service (`IAdminRoleAssignmentSafetyService` / `AdminRoleAssignmentSafetyService`) exists for assign, revoke, and disable validation; it performs conservative read-only checks, does not mutate role assignments, does not create audit events, and is not wired into authorization. An audit-event writer service (`IAdminRoleAssignmentAuditService` / `AdminRoleAssignmentAuditService`) exists as an isolated operation seam; it appends only to `admin_role_assignment_events` and does not assign or revoke roles. The guarded assign and revoke endpoints still fail closed when actor mapping is unavailable. A guarded backend-only disable-admin endpoint (`POST /api/admin/role-assignments/disable-admin`) now exists. It disables only an existing persistent `AdminUser`, uses the trusted actor resolver and internal write service, does not trust actor admin id or actor role ids from the request body, does not create `AdminUser` rows or invites, does not revoke roles automatically, does not add Admin UI role management, and does not activate persistent roles globally. A guarded backend-only provision-admin-user endpoint (`POST /api/admin/role-assignments/provision-admin-user`) now exists. It provisions only a persistent `AdminUser` mapping for an existing app user, uses the trusted actor resolver and internal provisioning service, does not trust actor id or actor roles from the request body, does not accept email-based provisioning, does not assign roles, does not create app users or invites, does not add Admin UI role management, and does not activate persistent roles globally. Persistent roles are still not used by `AdminPermissionAuthorizationHandler`, create-admin/invite endpoints do not exist, Admin UI role management still does not exist, and BootstrapAdmin remains the controlled-testing fallback. Production Admin RBAC remains incomplete until invite/enable workflows, operational UI, persistent role evaluation, endpoint-level enforcement, and rollback procedures are completed.

Production Admin RBAC is still incomplete. The current controlled-testing model keeps BootstrapAdmin as the effective admin source, with exactly three safe read-only Admin endpoints migrated to permission policies and dangerous/write/billing/CMS/Premium/free-lesson/user-level endpoints still protected by BootstrapAdmin. This plan describes the future persistence, safety, audit, rollout, validation, and rollback requirements before production role assignment can be enabled.

## Proposed future data model

The names below are conceptual table/entity names only. They must not be treated as implemented schema until a later EF entity and migration task explicitly adds them.

### `admin_users`

Purpose: represent an application user or invited email that is eligible to hold production Admin roles independently from the temporary BootstrapAdmin allow-list.

Important fields:

- `id`: stable admin-user primary key.
- `user_id` or normalized `email`: link to the existing user identity. Prefer `user_id` once the user exists; allow an email-only pending state only if the future invite flow requires it.
- `status`: active, disabled, pending_invite, or equivalent controlled values.
- `created_at_utc` and `updated_at_utc`.
- `disabled_at_utc` nullable.
- `created_by_admin_user_id` nullable for the initial owner/bootstrap seed case.

Uniqueness constraints:

- At most one active admin-user row per linked `user_id`.
- If email-only pending rows are allowed, normalized email must be unique among non-disabled pending/active admin-user rows.
- Status values should be constrained to the supported enum/string set.

Indexes:

- Unique/filtered lookup by `user_id` for active or non-disabled admin users.
- Unique/filtered lookup by normalized email for pending/active admin users, if email-only invites are supported.
- Index on `status` for diagnostics and owner-count checks.
- Index on `created_at_utc` for operational review.

Soft-delete/deactivation behavior:

- Prefer disabling/deactivation over hard deletion so historical role events and audit logs remain explainable.
- Disabled admins must not be able to access Admin endpoints, even if old role rows still exist.
- Re-enabling an admin should require a new audited action and should not silently restore revoked roles unless explicitly designed and audited.

Audit requirements:

- Create audit events for creation, disable, re-enable, identity link changes, and failed attempts where a safety invariant blocks the operation.
- Record actor admin id, target admin id, action type, reason, timestamp, result, and safe metadata only.

Must never store:

- Passwords, password hashes copied from the user table, JWTs, refresh tokens, invite tokens in plaintext, Paddle secrets, webhook secrets, connection strings, certificates, raw provider payloads, or broad unsanitized request bodies.

### `admin_user_roles`

Purpose: record role assignment history for admin users while allowing active-role queries and historical revocation review.

Important fields:

- `id`: stable role-assignment primary key.
- `admin_user_id`: target admin user.
- `role_id`: stable role id from `AdminRoleConstants` such as `super_admin`, `support`, `content_editor`, `billing_support`, or `read_only_auditor`.
- `assigned_at_utc`.
- `assigned_by_admin_user_id`.
- `reason`: required operator-entered reason for the assignment.
- `revoked_at_utc` nullable.
- `revoked_by_admin_user_id` nullable.
- `revoke_reason` nullable, required when revoking.

Uniqueness constraints:

- At most one active, non-revoked row per `(admin_user_id, role_id)`.
- `role_id` must be constrained to supported production role ids.
- `reason` must be required for assignment; `revoke_reason` must be required for revocation.

Indexes:

- Active-role lookup by `(admin_user_id, revoked_at_utc)`.
- Owner/Super Admin safety lookup by `(role_id, revoked_at_utc)` plus active admin status.
- Assignment history lookup by `assigned_at_utc` and `revoked_at_utc`.
- Actor lookup by `assigned_by_admin_user_id` and `revoked_by_admin_user_id` for investigations.

Soft-delete/deactivation behavior:

- Do not hard-delete role rows during normal operations.
- Revoke roles by setting revocation fields so the historical assignment remains available.
- Disabling an admin should cause effective role evaluation to return no roles, without erasing assignment history.

Audit requirements:

- Every assignment and revocation must create an immutable audit event.
- Dangerous role assignments, especially `super_admin` or any future role with role-management or billing/Premium powers, require audit logging before the operation is considered successful.

Must never store:

- Secrets, credentials, tokens, invite tokens in plaintext, provider secrets, full raw provider payloads, connection strings, or unredacted request bodies in `reason`, `revoke_reason`, or metadata.

### `admin_role_assignment_events` / `admin_role_audit_events`

Purpose: provide an immutable audit trail for admin role lifecycle decisions, safety-invariant denials, and operational diagnostics.

Important fields:

- `id`: stable event primary key.
- `actor_admin_user_id`: admin attempting or performing the action.
- `target_admin_user_id`: admin affected by the action.
- `action_type`: assign_role, revoke_role, disable_admin, enable_admin, invite_created, invite_revoked, last_owner_blocked, self_escalation_blocked, or equivalent controlled values.
- `role_id` nullable when the event is not role-specific.
- `reason`: required for role changes and safety-sensitive admin changes.
- `old_roles` / `new_roles` or safe snapshots if available.
- `occurred_at_utc`.
- `result`: succeeded, denied, failed_validation, failed_conflict, or equivalent controlled values.
- `safe_metadata_json` nullable.

Uniqueness constraints:

- Events are append-only and generally do not require business uniqueness beyond primary key.
- `action_type` and `result` should be constrained to supported values.

Indexes:

- Chronological index on `occurred_at_utc`.
- Target investigation index on `(target_admin_user_id, occurred_at_utc)`.
- Actor investigation index on `(actor_admin_user_id, occurred_at_utc)`.
- Role investigation index on `(role_id, occurred_at_utc)`.
- Result/action index on `(action_type, result, occurred_at_utc)` for denied-operation review.

Soft-delete/deactivation behavior:

- Audit events must be append-only. They should not be soft-deleted as part of normal admin operations.
- Any retention or archival policy must preserve the ability to investigate role assignments and last-owner safety decisions.

Audit requirements:

- This table is the canonical audit trail for role assignment persistence once implemented.
- Failed and denied safety-sensitive attempts should be recorded when safe to do so.

Must never store:

- Passwords, JWTs, refresh tokens, plaintext invite tokens, Paddle secrets, webhook secrets, connection strings, certificates, full raw provider payloads, full raw HTTP requests, or unsanitized exception dumps.

### Optional `admin_invites`

Purpose: support a future owner-managed invite flow for admins who do not yet have a linked app user or whose admin access needs explicit acceptance.

Important fields:

- `id`: stable invite primary key.
- `email`: normalized invited email.
- `role_id`: initial role proposed by the owner/super admin.
- `invited_by_admin_user_id`.
- `invite_status`: pending, accepted, expired, revoked, or equivalent controlled values.
- `expires_at_utc`.
- `accepted_at_utc` nullable.
- `revoked_at_utc` nullable.

Uniqueness constraints:

- At most one pending invite per normalized email and role, or stricter at most one pending invite per normalized email if product policy prefers a single invite at a time.
- `role_id` and `invite_status` must be constrained to supported values.

Indexes:

- Pending invite lookup by normalized email and status.
- Expiration lookup by `(invite_status, expires_at_utc)`.
- Inviter lookup by `(invited_by_admin_user_id, expires_at_utc)`.

Soft-delete/deactivation behavior:

- Do not hard-delete invites during normal operations; mark them accepted, expired, or revoked.
- Expired or revoked invites must not grant access.

Audit requirements:

- Invite creation, acceptance, expiration, and revocation must be auditable.
- Invite acceptance that creates or updates an admin user must create role-assignment audit events too.

Must never store:

- Plaintext invite tokens. If invite tokens are used, store only a strong hash with appropriate expiry and single-use semantics.
- Do not store passwords, JWTs, refresh tokens, Paddle secrets, webhook secrets, connection strings, or full raw request/provider payloads.

## Safety invariants

- At least one active Owner / Super Admin must always remain.
- Only Owner / Super Admin can manage admin roles.
- Owner / Super Admin cannot accidentally remove their own last owner access.
- Role changes require a human-readable reason.
- Dangerous role assignments require audit logging.
- Support, Content Editor, Billing Support, and Read-only Auditor roles cannot escalate themselves.
- Disabled admins cannot access Admin endpoints.
- Role assignment changes must not depend on Admin UI only; backend authorization and service-layer invariants must enforce them.
- BootstrapAdmin remains a controlled-testing fallback until production RBAC is fully enabled and explicitly switched over.

## Recommended rollout sequence

1. Add EF entities and a migration for admin role assignment tables only, with no runtime authorization behavior change.
2. Add a repository/service for reading assigned roles, but keep BootstrapAdmin as the effective source for controlled testing.
3. Add a read-only diagnostics endpoint for current admin role assignments, protected by `AdminRolesManage` or an Owner-only policy.
4. Add role assignment write endpoints only after audit logging and safety invariants are implemented and deliberately wired into those writes.
5. Switch `AdminPermissionAuthorizationHandler` to evaluate persistent assigned roles for production admins, while preserving the controlled BootstrapAdmin fallback until owner-approved cutover.
6. Migrate endpoints endpoint-by-endpoint from BootstrapAdmin to permission policies.
7. Disable or narrow BootstrapAdmin for public production only after owner-approved validation and a rollback plan.

## Audit requirements

Every role assignment and revocation must record:

- actor admin id;
- target admin id;
- role id;
- old roles / new roles if available;
- action type;
- reason;
- timestamp;
- result;
- safe metadata only.

Audit logs must not contain:

- passwords;
- JWTs;
- refresh tokens;
- invite tokens in plaintext;
- Paddle secrets;
- webhook secrets;
- connection strings;
- full raw provider payloads;
- full raw HTTP request bodies;
- certificates or private keys;
- unredacted exception dumps.

## Validation requirements before enabling production role assignment

- Migration SQL reviewed before production.
- Backup completed before migration.
- One known Owner / Super Admin exists and is active.
- Current BootstrapAdmin fallback still works during migration.
- Role assignment audit entries are created for assignment, revocation, denied escalation, and last-owner-removal attempts.
- Last-owner-removal is blocked.
- Disabled admin cannot access Admin endpoints.
- Support, Content Editor, Billing Support, and Read-only Auditor cannot access forbidden actions.
- Endpoint-level permission tests cover allowed and denied access before each endpoint migration.
- No Desktop, packaging, Paddle, subscription, entitlement, trial, Premium, free lesson, or lesson behavior changes are bundled into the role-assignment rollout.

## Rollback requirements

- Keep code rollback and migration compatibility plans documented before production migration.
- The initial schema migration should be additive so older BootstrapAdmin-based code can continue to run during rollback.
- Do not make persistent role assignments the only admin access path until owner-approved cutover is complete.
- Preserve BootstrapAdmin fallback during early rollout so controlled operators can recover access if persistent-role evaluation fails.
- If role-management endpoints are later added, rollback must include a way to freeze writes while preserving audit history.
- Rollback procedures must not require deleting audit records, dropping evidence, or committing SQL dumps/secrets to the repository.
## 2026-06-20 foundation update

The first database-only foundation for this plan is valid only when the EF migration set is complete and generated/validated by EF tooling: the `AddAdminRoleAssignmentPersistence` migration file, its matching `*.Designer.cs` metadata file, and `AppDbContextModelSnapshot.cs` must all include `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`. `dotnet ef migrations list` must show the migration, `dotnet ef migrations has-pending-model-changes` must report no pending changes, and the desktop release gate must pass with `-IncludeEfChecks` before this foundation is considered synchronized.

The foundation remains intentionally inactive at runtime. Persistent roles are not evaluated by `AdminPermissionAuthorizationHandler` yet. The audit-event writer exists for future role assignment operations, but it writes only `admin_role_assignment_events`, does not assign/revoke roles, and is not called by endpoints yet. No role assignment endpoints, Admin UI role management, invite flow, production admin seeding, or real admin emails were added. BootstrapAdmin remains the controlled-testing fallback, and production Admin RBAC remains incomplete until persistent role evaluation, write endpoints, safety checks wired into writes, and endpoint-level enforcement are completed.

## 2026-06-20 read-only diagnostics update

A protected read-only backend diagnostics endpoint now exists at `GET /api/admin/role-assignments/diagnostics` for inspecting aggregate persistent Admin role assignment state. It is protected by the Admin role-management permission policy and returns safe counts plus minimal per-admin identifiers/status/role ids; it does not return emails, invite tokens, raw audit payloads, secrets, or provider metadata.

This endpoint does not assign roles, revoke roles, create admin users, create invites, or write audit events. Persistent roles are still not active in authorization, no Admin UI role management exists yet, and BootstrapAdmin remains the controlled-testing fallback. Production Admin RBAC remains incomplete until persistent role evaluation, role assignment write endpoints, audit safety checks, and endpoint-level enforcement are completed.

### Internal role assignment write service seam

An internal backend-only `AdminRoleAssignmentWriteService` now exists as the next controlled Production Admin RBAC foundation seam for future role assignment operations. It is not exposed through HTTP endpoints and is not used by the Admin UI, Desktop, or runtime authorization. The service calls the validation-only safety service before assign, revoke, or disable mutations and appends audit events through the audit service for successful operations and validation/conflict failures.

Role assignment write endpoints still do not exist, Admin UI role management still does not exist, and persistent Admin roles are still not active in authorization decisions. BootstrapAdmin remains the controlled-testing fallback. Production Admin RBAC remains incomplete until write endpoints, endpoint-level enforcement, persistent role evaluation, and UI/operational workflows are completed.

## 2026-06-21 backend-only revoke endpoint update

The first narrowly scoped external role-assignment mutation endpoint exists at `POST /api/admin/role-assignments/revoke`. It is protected by `AdminRoleManagementPermissionPolicyName`, accepts only the target admin user id, role id, reason, and optional safe metadata, and routes revocation through the internal `IAdminRoleAssignmentWriteService.RevokeRoleAsync` seam instead of mutating EF entities or writing audit events in the endpoint handler. The endpoint now calls the trusted actor resolver to derive the persistent actor AdminUser id and actor role ids from server-side authenticated identity only; the request body still does not accept actor admin ids or actor role ids.

If the authenticated principal cannot be mapped to an active persistent AdminUser with active persistent role ids, the revoke endpoint still fails closed with `admin_role_assignment_actor_mapping_unavailable`. This revoke update did not add disable-admin, create-admin, or invite endpoints, and it does not add Admin UI role management. Persistent roles are still not active in global runtime authorization. Production Admin RBAC remains incomplete until persistent role evaluation, complete write workflows, operational UI, and endpoint-level enforcement are completed.

## 2026-06-21 backend-only assign endpoint update

A guarded backend-only assign endpoint now exists at `POST /api/admin/role-assignments/assign`. It assigns a persistent Admin role only to an existing persistent `AdminUser`; it does not create `AdminUser` rows, does not create invites, does not assign by email, and does not add Admin UI role management. The endpoint is protected by `AdminRoleManagementPermissionPolicyName`, accepts only `targetAdminUserId`, `roleId`, `reason`, and optional `safeMetadataJson`, derives the actor AdminUser id and actor role ids through `IAdminRoleAssignmentActorResolver`, and does not trust actor id or actor role values from the request body.

The endpoint delegates assignment to `IAdminRoleAssignmentWriteService.AssignRoleAsync` and does not mutate EF entities, call `SaveChanges`, or write audit events directly. Persistent roles are still not activated globally: `AdminPermissionAuthorizationHandler` still does not evaluate persistent role-assignment tables or read/safety/audit/write/actor/bootstrap services. Production Admin RBAC remains incomplete until disable/create/invite workflows, operational UI, persistent role evaluation, endpoint-level enforcement, and rollback procedures are completed.

### Internal first-owner bootstrap seam (current)

An internal backend-only `IAdminRoleAssignmentBootstrapService` / `AdminRoleAssignmentBootstrapService` seam exists for the controlled first-owner bootstrap workflow. It is now invoked only by the controlled bootstrap HTTP endpoint described below and does not add Admin UI role management.

The service can create only the first persistent owner-equivalent mapping for the current authenticated app user under strict conditions. The project currently uses `AdminRoleConstants.SuperAdmin` as the initial Owner/SuperAdmin-equivalent role. The service fails closed for missing app-user identity, missing reason, any existing active non-disabled SuperAdmin assignment, disabled conflicting mappings for the same app user, active same-app-user mappings that already have active roles, and normalized-email conflicts with a different active Admin user.

This seam does not activate persistent roles globally. Persistent role assignments are still not used by `AdminPermissionAuthorizationHandler`, and BootstrapAdmin access behavior remains unchanged. Production Admin RBAC remains incomplete until complete write workflows, operational Admin UI, persistent role evaluation, and endpoint-level enforcement are completed.

### Controlled first-owner bootstrap HTTP endpoint

A controlled backend-only endpoint now exists at `POST /api/admin/role-assignments/bootstrap-first-owner` for bootstrapping only the first persistent Owner/SuperAdmin-equivalent Admin mapping for the current authenticated admin user. The endpoint is protected by `AdminRoleManagementPermissionPolicyName`, derives the app user id and optional trusted email only from server-side authenticated claims, and does not trust `appUserId`, email, role, target, or actor fields from the request body.

This addition does not add disable-admin, create-admin, invite, or Admin UI role-management workflows. It also does not activate persistent roles globally: `AdminPermissionAuthorizationHandler` still does not evaluate persistent role-assignment tables or read/safety/audit/write/actor/bootstrap services. Production Admin RBAC remains incomplete until complete write workflows, operational UI, persistent role evaluation, endpoint-level enforcement, and rollback procedures are completed.

## 2026-06-21 backend-only disable-admin endpoint update

A guarded backend-only disable-admin endpoint now exists at `POST /api/admin/role-assignments/disable-admin`. It disables only an existing persistent `AdminUser`, is protected by `AdminRoleManagementPermissionPolicyName`, accepts only `targetAdminUserId`, `reason`, and optional `safeMetadataJson`, derives the actor AdminUser id and actor role ids through `IAdminRoleAssignmentActorResolver`, and does not trust actor id or actor role values from the request body.

The endpoint delegates disablement to `IAdminRoleAssignmentWriteService.DisableAdminAsync` and does not mutate EF entities, call `SaveChanges`, revoke roles automatically, create `AdminUser` rows, create invites, or write audit events directly. It does not add create-admin, invite, enable-admin, or Admin UI role-management workflows. Persistent roles are still not activated globally: `AdminPermissionAuthorizationHandler` still does not evaluate persistent role-assignment tables or read/safety/audit/write/actor/bootstrap services. Production Admin RBAC remains incomplete until create/invite workflows, operational UI, persistent role evaluation, endpoint-level enforcement, and rollback procedures are completed.

### Internal AdminUser provisioning service seam

An internal backend-only `IAdminRoleAssignmentAdminUserProvisioningService` / `AdminRoleAssignmentAdminUserProvisioningService` seam now exists for provisioning an additional persistent `AdminUser` mapping for an existing application user. It creates only the `AdminUser` mapping needed before a future role assignment can occur; it does not assign roles, create `AdminUserRole` rows, create application users, create invites, enable disabled admins, or expose any HTTP endpoint.

The service validates that the future server-derived actor is Owner/SuperAdmin-equivalent, requires a non-empty reason, fails closed on duplicate active mappings, disabled/inactive existing mappings, missing target app-user identity, and normalized-email conflicts, and writes audit events through `IAdminRoleAssignmentAuditService`. This addition does not add Admin UI role management, does not activate persistent roles globally, and does not change `AdminPermissionAuthorizationHandler`; persistent roles are still not used by that handler or by global runtime authorization.

Production Admin RBAC remains incomplete until a guarded create-admin/provisioning endpoint, operational UI, persistent role evaluation, endpoint-level enforcement, and rollback procedures are completed.
