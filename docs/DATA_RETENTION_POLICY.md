# Data Retention and Storage Policy (Draft)

Review date: 2026-08-03.

This is a technical product retention policy draft (not a legal policy).

## Deployed disabled Google Play pending-refund review material

Pending-refund notifications persist only SHA-256 fingerprints plus a separately protected token/order payload; raw pending-refund tokens, order IDs, Pub/Sub bodies, and obfuscated account/profile identifiers are not stored. A successful review clears its protected payload immediately. Permanent failures retain protected payload only until the configured bounded terminal retention deadline, after which cleanup clears it while retaining safe audit metadata and fingerprints. The schema is deployed through `20260803052655_AddGooglePlayPendingRefundReviewFoundation`, but runtime processing remains disabled; production is `0.1.35-backend.139`, with Google Play Billing, RTDN, reconciliation, and pending-refund review disabled.

## Implemented persistence scope

Backend persistence foundation (PostgreSQL + EF Core) is implemented for:

- `users`
- `user_profiles`
- `user_settings`
- `lesson_sessions`
- `lesson_messages`
- `lesson_summaries`
- `usage_events`
- `daily_usage_counters`
- `feedback_results`
- subscription, entitlement, Paddle webhook event, subscription snapshot, and payment persistence tables from the confirmed EF migrations

## Stored now (product)

- Lesson messages/transcript text may be stored as learning history.
- Lesson summaries may be stored as learning history.
- Feedback results may be stored as learning history.
- Usage event metadata and daily aggregated counters may be stored.
- Lesson sessions include active/finished/abandoned state and `LastHeartbeatAtUtc` for backend-enforced single active lesson protection.
- Paddle webhook events, subscription snapshots, payment snapshots, and entitlement records may be stored by backend billing foundations.

## Sensitive/auth data handling

- Passwords are stored only as backend password hashes.
- JWT tokens are not stored in backend persistence tables.
- Desktop still uses a local `auth-session.json` file under the app data folder, but the current Windows implementation stores a DPAPI-protected Base64 payload with `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`.
- After DPAPI unprotects the payload for the same Windows user, the session contains access token, token type, expiry, and user DTO fields.
- The storage service can migrate an old plaintext JSON payload by reading it once and saving it back as a protected payload.
- Documentation and support instructions must not describe current desktop auth storage as raw plaintext token storage.

## Backend-only secrets and AI provider keys

- `OPENAI_API_KEY` is backend-only and is needed only for real AI/TTS/STT testing.
- The key must never be added to desktop settings, tester packages, docs with real values, scripts, source files, or committed files.
- The key must never be sent to testers.
- Desktop only needs a Backend URL and must call backend APIs only.

## Not stored now (product)

- Raw audio is not intentionally persisted as backend learning history.
- Full prompts are not persisted as lesson history.
- Real provider secrets/API keys are not persisted in source control.

## Development free-limit note

- Development can run diagnostics-only free-limit mode (`FreeLimits:EnforcementEnabled=false`).
- In this mode, counters still increment while lesson actions are not blocked.

## Future work (not implemented)

- Production-wide auth enforcement for all runtime endpoints.
- Production billing operations completion.
- Roles/authorization layers for production operations.
- CMS/Admin workflow: development/admin Admin CMS Content exists, and successful Admin CMS Save draft operations write bounded audit rows with actor identity, UTC timestamp, content pack, entity type/stable key, changed fields, hashes, source/status/reason, and request/correlation id when available. Smoke/test audit entries are hidden by default in the Admin CMS Audit subtab and can be shown with the debugging checkbox. Full edited values, secrets, tokens, passwords, provider keys, webhook secrets, SMTP passwords, and bearer tokens must not be retained in audit rows. Unsaved CMS content is not retained in browser storage or URL hash. Production RBAC and critical-change approval remain future work.
- Contabo deployment.
