# CMS / Admin Planning (Later Phase)

Review date: 2026-06-01.

## Current decision

- CMS/Admin should **not** be built now.
- CMS/Admin operational readiness remains deferred until desktop hardening is complete.
- Prompt, scenario, dialogue, and bot-behavior quality polishing is intentionally deferred to CMS/Admin.
- Current desktop hardening must not rewrite lesson JSON or polish prompts in code.

## Current foundation

A local Development admin support foundation exists for controlled diagnostics/support work, with existing smoke/audit coverage. It is not a full production CMS/Admin system and does not make public release ready.

Relevant scripts:

- `tools/smoke_admin_foundation.ps1`
- `tools/audit_admin_shell.ps1`

## Long-term CMS/Admin scope

Eventually, CMS/Admin capabilities may manage:

- lesson scenarios and content versions;
- prompts and tutor behavior;
- tutor avatars;
- users/accounts support visibility;
- subscriptions/payments/entitlements visibility;
- optional usage/support diagnostics;
- safe content hotfix workflow.

Study language expansion, Interface language expansion, and Native/Explanation language catalog changes require explicit future product decisions and are not part of this documentation sync.

## Security and roles

- CMS/Admin must require authentication.
- CMS/Admin must enforce admin/content-manager/support roles as appropriate.
- Production users/admins should never edit PostgreSQL directly.
- Do not store provider secrets, OpenAI API keys, webhook secrets, or other real secrets in CMS/Admin content or tracked repository files.

## Content workflow requirements

CMS content lifecycle should include:

- draft/published states;
- validation;
- preview;
- versioning;
- rollback;
- audit trail;
- safe prompt editing controls.

## Recommended sequence

1. Finish remaining desktop release hardening.
2. Complete production billing readiness only after desktop hardening.
3. Define minimum production support/admin needs.
4. Add production roles/RBAC and operational audit requirements.
5. Add backend content models/versioning only after the operational plan is approved.
6. Build CMS/Admin UI after the backend and safety model are clear.

## MVP boundary (now)

Current MVP should keep:

- lesson JSON/static content approach;
- backend as source of truth for account, lesson history, active lesson, subscription/access, entitlements, payments, and AI/TTS/STT calls;
- desktop-only tester package hardening as the current priority.

## Tooling notes

- Do **not** use Prisma Studio as a CMS.
- DBeaver is acceptable for local database inspection only.
- Full CMS/Admin production operations are not ready yet.
