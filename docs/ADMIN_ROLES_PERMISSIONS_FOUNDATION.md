# Admin Roles/Permissions Foundation

Review date: 2026-07-01.

## Current status

The backend now has a stable admin roles/permissions foundation, Admin Shell role-aware UI, persistent Admin role management, and completed Production Admin RBAC / persistent role authorization after backend `0.1.35-backend.88`. Current production behavior for migrated AdminPermission endpoints is persistent-role based with BootstrapAdmin fallback disabled.

Stable production role constants now exist for:

- `super_admin`
- `support`
- `content_editor`
- `billing_support`
- `read_only_auditor`

Legacy alias constants remain mapped to the new stable target role ids for compatibility with older foundation terminology.

Stable permission constants now exist for admin self/capabilities, users, audit, CMS, runtime status, subscriptions diagnostics, premium grant/revoke, free lesson allowance reset, billing diagnostics, and product statistics.

A static production role-to-permission catalog exists for Owner/Super Admin, Support, Content Editor, Billing Support, and Read-only Auditor, and a static Admin endpoint/action-to-permission catalog documents the permission that protects each current Admin action plus future-only seams. Production role assignment persistence, database tables, and Admin UI role management now exist; no EF migration was added or run for the .88 RBAC completion stage. Migrated AdminPermission endpoints enforce persistent role permissions with fallback disabled; BootstrapAdmin-only endpoints remain intentionally separate.

## Exposed admin metadata

`GET /api/admin/me` now exposes:

- `roles`
- `permissions`
- `isBootstrapAdmin`

`GET /api/admin/capabilities` now exposes:

- `roles`
- `permissions`

`ProductionRolesAvailable` is now treated as the signal that persistent Admin role authorization is active; it is not a paid-launch or broad public-readiness flag.

`CmsUiAvailable` means the Admin Shell has the current CMS Content workspace wired to the CMS content-pack read endpoints guarded by `cms.content.read`. It is a System capability for the Admin CMS Content UI surface, not a signal that the signed-in role can see the tab, not a CMS runtime published-snapshot health check, and not a CMS content-publish readiness flag. Per-role CMS tab visibility still comes from `GET /api/admin/me` permissions and the Admin Shell permission checks.

## Admin Shell UI-awareness

The Admin Shell now loads `GET /api/admin/me` and `GET /api/admin/capabilities` and renders the returned metadata informationally.

Completed UI-awareness behavior:

- Overview shows admin source, environment, checked timestamp, Bootstrap admin status, role badges, and permission count.
- Overview includes a **Roles and permissions** card.
- Available workflows are rendered from permissions for visibility only.
- Role visibility and workflow availability match the backend permission catalog.
- Backend authorization remains the security boundary; UI visibility is usability only.
- Billing/Paddle live payment completion remains unavailable/deferred.

This is role-aware UI backed by server-side persistent-role authorization for the migrated production RBAC surface. UI hiding alone must still never be treated as authorization.

## Boundaries and deferred work

Production role management/RBAC is complete for the verified production role matrix after backend `0.1.35-backend.88`. Do not claim this completes paid public launch.

Deferred work:

- actor-centric Admin Activity / Audit Log by admin user;
- critical-change approval remains future hardening;
- production billing/Paddle live payment verification remains deferred;
- broad public production launch remains deferred.

## Deployment note

Production Admin RBAC / persistent role management is completed in production backend `0.1.35-backend.88`, active at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.88` with `/opt/languagevoicetutor/backend/current` pointing to that release at the last verification. No EF migration was required for this RBAC completion stage. Backend `/health` and `/api/health/database` returned `200 Healthy`.
