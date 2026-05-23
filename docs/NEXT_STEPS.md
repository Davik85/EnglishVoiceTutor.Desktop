# Next Steps

Review date: 2026-05-22.

This roadmap starts from the current confirmed MVP state where desktop/backend builds pass, lesson content audit passes, persistence foundation is active, feedback_results persistence is validated, and Development uses diagnostics-only free-limit mode (`FreeLimits:EnforcementEnabled=false`).

## Recommended next backend/product order

1. Final small stabilization pass
   - monitor STT quality with real short learner phrases
   - harden TutorIdentityGuard / tutor identity behavior if warnings continue
2. Desktop login UI and wiring to authenticated endpoints (`/api/me/settings`)
3. Subscription/payment enforcement
4. CMS/admin panel only after auth, roles, content versioning, draft/published workflow, audit trail, and rollback

## Notes on current free-limit mode

- Free-limit counters and diagnostics are implemented and should stay active in local development.
- Local development should keep enforcement disabled (`FreeLimits:EnforcementEnabled=false`) to avoid blocking MVP testing.
- Enforcement can be re-enabled later by configuration for subscription/payment rollout work.
- Do not treat current dev diagnostics as billing/subscription enforcement.

## Already completed (do not relist as future work)

- read-only free-limit diagnostics endpoint
- soft enforcement wiring
- desktop HTTP 429 UX
- feedback_results persistence wiring
- Development diagnostics-only free-limit mode
