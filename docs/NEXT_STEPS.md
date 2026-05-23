# Next Steps

Review date: 2026-05-23.

This roadmap starts from the current confirmed MVP state where:
- desktop/backend builds pass,
- lesson content audit passes,
- auth/JWT foundation is implemented,
- optional desktop Account UI is implemented,
- Settings and Lesson Chat runtime persistence are auth-aware,
- Development free-limit mode is diagnostics-only.

## Recommended next backend/product order

1. Small auth/runtime cleanup
   - reduce noisy duplicate-email logs if needed
   - review expired-token fallback behavior
   - keep dev fallback safe for local testing
2. Start subscription/payment foundation only after current auth/runtime identity is stable.
3. Add subscription/payment enforcement later.
4. Add CMS/admin panel later, only after:
   - roles
   - content versioning
   - draft/published workflow
   - audit trail
   - rollback
   - safe prompt/scenario editing

## Already completed (do not relist as future work)

- backend Auth/JWT foundation
- optional desktop Account UI
- authenticated user settings endpoints
- auth-aware Settings source switching (`/api/dev/user-settings` <-> `/api/me/settings`)
- auth-aware Lesson Chat runtime persistence
- read-only free-limit diagnostics
- Development diagnostics-only mode
