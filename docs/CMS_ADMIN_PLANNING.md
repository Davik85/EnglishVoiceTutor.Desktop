# CMS / Admin Planning (Later Phase)

## Current decision
- CMS/admin panel should **not** be built now.
- CMS is a later product/admin layer, not part of current MVP implementation.

## Long-term CMS scope
Eventually, CMS/admin capabilities should manage:
- lesson scenarios
- prompts
- tutor avatars
- study languages/content versions
- users/accounts
- subscriptions/payments visibility
- optional usage/support diagnostics

## Security and roles
- CMS must require authentication.
- CMS must enforce admin/content-manager roles.

## Content workflow requirements
CMS content lifecycle should include:
- draft/published states
- versioning
- rollback
- audit trail
- safe prompt editing controls

## Recommended sequence
1. finish persistence foundation
2. add auth/JWT and role model
3. add backend content models and versioning
4. only then build admin UI/CMS

## MVP boundary (now)
Current MVP should keep:
- lesson JSON/static content approach
- backend dev endpoints for persistence reads/writes

## Tooling notes
- Do **not** use Prisma Studio as a CMS.
- DBeaver is acceptable for local database inspection only.
- Production users/admins should never edit PostgreSQL directly.
