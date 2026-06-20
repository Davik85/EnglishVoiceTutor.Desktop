# Admin Role Assignment Persistence Plan

Review date: 2026-06-20.

Scope: planning/model design only. This document does not add runtime role persistence, EF entities, EF migrations, database tables, DbContext changes, authorization behavior changes, Admin UI role management, role assignment endpoints, endpoint policy migrations, billing/Paddle changes, entitlement changes, Desktop changes, packaging changes, generated artifacts, or secrets.

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
4. Add role assignment write endpoints only after audit logging and safety invariants are implemented.
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

The first database-only foundation for this plan now exists: EF entities, DbSet mappings, and an additive migration create `admin_users`, `admin_user_roles`, and `admin_role_assignment_events` for future Admin role assignment persistence. This foundation is intentionally inactive at runtime.

Persistent roles are not evaluated by `AdminPermissionAuthorizationHandler` yet. No role assignment endpoints, Admin UI role management, invite flow, production admin seeding, or real admin emails were added. BootstrapAdmin remains the controlled-testing fallback, and production Admin RBAC remains incomplete until persistent role evaluation, write endpoints, audit safety checks, and endpoint-level enforcement are completed.
