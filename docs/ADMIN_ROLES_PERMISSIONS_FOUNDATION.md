# Admin Roles/Permissions Foundation

Review date: 2026-06-18.

## Current status

The backend now has a stable admin roles/permissions foundation, and the Admin Shell roles/permissions UI-awareness deployment is completed. Production role management/RBAC is not enabled yet. Current production behavior remains BootstrapAdmin-based.

Stable role constants now exist for:

- `super_admin`
- `support_agent`
- `content_manager`
- `finance_admin`
- `readonly_analyst`

Stable permission constants now exist for admin self/capabilities, users, audit, CMS, runtime status, subscriptions diagnostics, premium grant/revoke, free lesson allowance reset, billing diagnostics, and product statistics.

Bootstrap admins map to `super_admin`. Bootstrap admins currently receive the full permission set.

## Exposed admin metadata

`GET /api/admin/me` now exposes:

- `roles`
- `permissions`
- `isBootstrapAdmin`

`GET /api/admin/capabilities` now exposes:

- `roles`
- `permissions`

`ProductionRolesAvailable` remains `false`.

## Admin Shell UI-awareness

The Admin Shell now loads `GET /api/admin/me` and `GET /api/admin/capabilities` and renders the returned metadata informationally.

Completed UI-awareness behavior:

- Overview shows admin source, environment, checked timestamp, Bootstrap admin status, role badges, and permission count.
- Overview includes a **Roles and permissions** card.
- Available workflows are rendered from permissions for visibility only.
- Tabs, buttons, and backend calls are not blocked by the client-side permission view.
- The System tab shows `productionRolesAvailable=false` and keeps Billing/Paddle unavailable/deferred.

This is UI-awareness only. It is not UI role management, production RBAC, or endpoint-level per-role/per-permission enforcement.

## Boundaries and deferred work

This is only the foundation. Do not claim production role management/RBAC is complete.

Deferred work:

- production role management is not enabled yet;
- endpoint-level per-role/per-permission enforcement is not implemented yet;
- no UI role management exists yet;
- critical-change approval remains future work;
- production billing/Paddle readiness remains deferred;
- broad public production launch remains deferred.

## Deployment note

The foundation is present in production backend `0.1.35-backend.24`, active at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.24` with `/opt/languagevoicetutor/backend/current` pointing to that release at the last verification. No EF migration was required for this foundation. Backend `/health` and `/api/health/database` returned `200 OK`, and the backend service was active/running.
