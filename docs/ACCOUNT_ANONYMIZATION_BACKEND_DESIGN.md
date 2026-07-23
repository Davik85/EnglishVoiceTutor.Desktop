# Account anonymization backend technical design

> **Implementation status (2026-07-23).** The local backend and Admin Shell now implement synchronous Super-Admin execution through `POST /api/admin/feedback-reports/{reportId}/account-anonymization/execute`, guarded by the execute permission and Admin write limit, with one simple irreversible-action confirmation dialog. The required request contains only `operationId` and `preflightFingerprint`. It performs no Paddle or provider mutation, preserves financial/provider rows, and uses one local transaction plus post-mutation verification. Earlier draft requirements for two-person approval, typed confirmation, provider action tracking, external notification, and backup restore replay are superseded by the approved simple process. The combined migration and backend deployment remain pending; production is `0.1.35-backend.129`.

Companion to [Account anonymization procedure and data inventory](ACCOUNT_ANONYMIZATION_PROCEDURE.md). That procedure remains the canonical data-treatment and legal/operational runbook; this document specifies a future backend implementation shape without implementing it.

## 1. Status and implementation boundary

**Status: complete local technical implementation with the Slice 1 foundation production-deployed in `0.1.35-backend.129`.** The repository implements the Admin preflight panel, one confirmation dialog, execution endpoint, and local migration, but none of those accumulated changes are deployed. The combined migration and backend deployment remain pending. The local execution performs no provider action, notification, or financial mutation.

### Slice 1 deployed foundation and repository Admin UI

Slice 1 and migration `20260722132656_AddAccountAnonymizationPreflightFoundation` are production-deployed in `0.1.35-backend.129`. The repository-only accumulated release adds the complete Super-Admin preflight, confirmation, execution, and email-intake flow, plus migration `20260723045852_AddAccountAnonymizationExecution`; it is pending the combined `.130` deployment. `POST /api/admin/feedback-reports/{reportId:guid}/account-anonymization/preflight` creates or refreshes a read-only preflight, `GET /api/admin/feedback-reports/{reportId:guid}/account-anonymization` reads it, and `POST /api/admin/feedback-reports/{reportId:guid}/account-anonymization/execute` executes a fresh matching preflight. Both permissions are assigned only to `super_admin`.

The code-owned policy is `account_anonymization_policy_v2`, with a deterministic SHA-256 hash over safe category decisions. Preflights have a 15-minute lifetime: an unexpired `refresh=false` request returns the stored operation, while forced or expired refresh increments its preflight version. Preflight and email intake do not mutate learner, billing, authentication, entitlement, Admin, CMS, or provider records or call an external provider. Execution is the separate local transaction that removes approved learner data, retains financial/provider rows, and verifies completion. Active Premium blocks deletion until the paid period ends; operators should remind customers to cancel renewal, while refunds, disputes, and chargebacks remain manual support matters. No second-Admin approval is planned.

## 2. Recommended backend architecture

Use small, repository-style endpoint/service components rather than a generic workflow framework.

| Component | Responsibility | First slice? |
| --- | --- | --- |
| `AccountAnonymizationEligibilityGuard` | Resolve the report, require `account_deletion`, evaluate support-state and duplicate-operation eligibility. | Yes, read-only |
| `AccountAnonymizationPreflightService` | Produce deterministic safe counts, blockers, fingerprint, and expiry from `AppDbContext`; never mutate learner data. | Yes |
| `AccountAnonymizationOperation` / policy snapshot | Durable state, preflight version, safe counts/blockers and temporary target link. | Yes, schema only |
| Admin actor/permission guard | Use current persistent-role authorization and `IAdminRoleAssignmentActorResolver`; reject self/active/sole Admin targets. | Yes |
| Execution orchestrator | Validate final confirmation, acquire operation lock, coordinate later phases. | Later |
| Local mutation component | Perform ordered child-to-parent database deletion/redaction in one defined transaction. | Later |
| External reconciliation component | Coordinate provider-neutral billing adapters plus SMTP, OpenAI and backup actions that cannot join the local transaction. Billing adapter keys explicitly allow `paddle`, `google_play`, `apple_app_store`, and future approved providers; Google Pay and Apple Pay are not separate subscription-provider adapters. | Later, state only initially |
| Verification service | Execute safe post-commit checks and publish result. | Later |
| Completion evidence writer | Write minimal non-linkable evidence after target link is cleared. | Schema first; write later |
| Restore reconciliation service | Reapply completed operations to a restored copy before availability. | Later |

## 3. Exact proposed API contract

Use the existing Admin feedback-report route family. Proposed constants and endpoint mappings are future work only.

| Method and route | Authorization / rate limit | Request | Success response | Notes |
| --- | --- | --- | --- | --- |
| `POST /api/admin/feedback-reports/{reportId:guid}/account-anonymization/preflight` | `AdminPermission:account_anonymization.preflight.read`; existing `AdminRead` (or a future dedicated low-volume Admin preflight policy) | `{ refresh: boolean }` | `200` `AccountAnonymizationPreflightResponse` | Read-only inspection. Existing current fingerprint is returned when valid unless `refresh`; a newly persisted preflight foundation record is allowed. |
| `GET /api/admin/feedback-reports/{reportId:guid}/account-anonymization` | `AdminPermission:account_anonymization.preflight.read`; `AdminRead` | none | `200` `AccountAnonymizationOperationStatusResponse` | Returns latest operation/preflight safe state; no target identity/content. |
| `POST /api/admin/feedback-reports/{reportId:guid}/account-anonymization/execute` | `AdminPermission:account_anonymization.execute`; `AdminWrite` | `operationId`, `preflightFingerprint` | `200` completed safe operation status | Implemented locally; one browser confirmation is an operator safeguard, while backend validation remains authoritative. |
| `GET /api/admin/account-anonymization/operations/{operationId:guid}/verification` | `AdminPermission:account_anonymization.preflight.read`; `AdminRead` | none | `200` `AccountAnonymizationVerificationResponse` | **Later slice.** Safe result only. |

`AccountAnonymizationPreflightResponse` contains `operationId`, `reportId`, `state`, `preflightFingerprint`, `expiresAtUtc`, `categoryCounts: { key, count }`, `blockingReasons: [safeCode]`, `retentionStates: { immediateDeleteCount, restrictedRetentionCount, unresolvedCount }`, `externalActionState`, and `backupReconciliationState`. It must not return a target user ID, email, display name, reason, text, token, raw payload, or provider ID.

`AccountAnonymizationOperationStatusResponse` adds safe timestamps, state, category counts, blockers, external state, backup state, and verification state. `AccountAnonymizationVerificationResponse` adds only verification status, category-result counts, safe failure codes, provider/backup state, and completed timestamp.

The server resolves target user identity only from the report row during preflight/execution; clients never submit it. The execute contract contains only the current operation ID and preflight fingerprint; it has no typed confirmation, password, or acknowledgement field.

| Condition | Status / safe error code |
| --- | --- |
| No Admin session | `401` normal authentication response |
| Missing permission | `403` normal authorization response |
| Missing report | `404` `account_anonymization_report_not_found` |
| Wrong category | `409` `account_anonymization_not_deletion_request` |
| Non-eligible support state | `409` `account_anonymization_request_state_blocked` |
| Stale/expired fingerprint | `409` `account_anonymization_preflight_stale` |
| Typed value/acknowledgement invalid | `400` `account_anonymization_confirmation_invalid` |
| Duplicate key / operation running | `409` `account_anonymization_operation_in_progress` with safe status |
| Completed operation | `200` status for identical idempotent read, otherwise `409` `account_anonymization_already_completed` |
| Active/sole/self Admin target | `409` `account_anonymization_admin_target_blocked` |
| Unresolved retention | `409` `account_anonymization_retention_unresolved` |
| Renewal/refund/chargeback/legal-hold blocker | `409` `account_anonymization_billing_blocked` |
| Unknown/unclassified dependency | `409` `account_anonymization_dependency_unclassified` |

Execution must fresh-check permission, persistent actor mapping, report state, target, fingerprint, expiry, acknowledgement, typed value, idempotency key, and concurrency after browser confirmation. Browser confirmation is an accidental-click safeguard only, never the authorization boundary.

## 4. Permissions and Admin security

The implemented permissions are `account_anonymization.preflight.read` and `account_anonymization.execute`, with policies `AdminPermission:account_anonymization.preflight.read` and `AdminPermission:account_anonymization.execute`. Execution is limited to `super_admin`; the constants, policies, role mapping, and endpoint catalog are part of the local accumulated release.

Every route uses the Admin cookie scheme and existing persistent-role `AdminPermissionAuthorizationHandler`; endpoint code then uses `IAdminRoleAssignmentActorResolver` for a trusted Admin-user ID. It rejects missing mapping, self-target, active target Admin, and sole/last-admin cases. Recent re-authentication has no existing verified mechanism in the reviewed design and is therefore an execution blocker/design decision. Two-person approval remains an explicit decision; if adopted, the executor and approver must be distinct resolved Admin IDs.

Admin browser calls must preserve the repository's authenticated cookie/session protections and add the project's approved antiforgery/CSRF expectation before executing a write route; do not infer that cookie authentication alone is sufficient. Apply current `AdminRead` to reads and `AdminWrite` to confirmation/execution initially, then consider a low-limit per-admin execute policy. Rate limiting and CSRF reduce abuse, but neither replaces authorization, operation locking, or server-side revalidation.

## 5. Durable operation model

Future schema: `account_anonymization_operations`, `account_anonymization_policy_snapshots`, and (only once execution exists) `account_anonymization_completion_evidence`. Do not reuse `admin_actions` as the primary completion record because it has a required target-user relationship and reason/metadata risks.

| Entity.field | Type / nullability | Purpose and personal-data rule |
| --- | --- | --- |
| `operations.Id` | `uuid`, required PK | Random operation ID; safe to expose. |
| `ReportId` | `uuid`, required FK initially | Existing deletion support report. Clear/null or replace with non-linkable completion reference once report linkage is legally no longer required. |
| `TargetUserId` | `uuid`, nullable FK `Restrict` | Internal temporary target resolution only. Clear in the same committed completion transition; never copy to evidence. |
| `State` | bounded string, required | Proposed state-machine value; no personal data. |
| `PreflightVersion` / `PreflightFingerprint` | integer / SHA-256-style bounded string, required | Deterministic snapshot/version and staleness check; fingerprint is over safe normalized state, never email/content. |
| `ProcedureVersion` / `PolicySnapshotId` | bounded string / `uuid`, required | Procedure and legally reviewed decision version. |
| `ActorAdminUserId`, `ApproverAdminUserId` | `uuid`, actor required when action begins; approver nullable | Admin references, not learner data; retain/restrict per Admin audit policy. |
| `CreatedAtUtc`, `UpdatedAtUtc`, `StartedAtUtc`, `CompletedAtUtc` | timestamps; first two required | Lifecycle evidence. |
| `CategoryCountsJson` | bounded JSON, required | Named categories and integers only; reject IDs/content. |
| `BlockingCodesJson`, `FailureCode`, `VerificationState` | bounded JSON/string | Enumerated safe codes only. |
| `ExternalStatesJson`, `BackupReconciliationState` | bounded JSON/string | Provider-neutral external/backup state codes and safe references only. Each external billing item may contain only provider key, action type, state, safe result code, timestamps, and manual-review flag; it must contain no purchase token, receipt, transaction/customer/subscription ID, webhook/notification payload, signature, credential, secret, or other provider identifier. |
| `RowVersion` | provider concurrency token, required | Optimistic concurrency. |
| `IdempotencyKeyHash` | bounded hash, nullable until execute | Hash of client key; never store typed confirmation. |

`account_anonymization_policy_snapshots` stores only policy version, category key, treatment (`immediate_delete`, `restricted_retention`, `unresolved`), legal-source reference, mandatory-period descriptor, approved-purpose code, created timestamp, and version hash. It must contain no person or provider payload. Completion evidence stores operation ID, procedure/policy versions, safe actor/approver audit references, timestamps, counts, result/failure codes, external/backup states, and no report/user link after completion. A restore reconciliation manifest uses completed operation IDs and safe policy/version/result state only; it cannot map back to a learner.

Indexes: unique active operation per `ReportId`; unique active operation per non-null `TargetUserId`; unique `IdempotencyKeyHash` scoped to operation/actor; index `(State, UpdatedAtUtc)`; unique preflight `(ReportId, PreflightFingerprint)` while current; and row-version concurrency. Use `Restrict` FKs during active processing; do not cascade-delete evidence.

## 6. State machine

Operation state is separate from the existing feedback status list.

| State / transition | Actor and conditions | DB/retry/support effect | Reversible? |
| --- | --- | --- | --- |
| `preflight` | Read-permitted Admin; valid account-deletion report | Create/refresh safe snapshot; retry refresh replaces only current safe preflight. Support remains unchanged. | Yes |
| `blocked` | Guard/preflight finds blocker | Store safe code/counts; re-preflight may move to `preflight`/`ready`. Support normally remains `reviewed`/`processing`. | Yes |
| `awaiting_approval` / `awaiting_external_action` | Required legal/security/provider condition missing | Safe pending state only; retry after recorded decision/action. | Yes |
| `ready` | All execution gates pass and fresh preflight valid | Persist readiness/fingerprint; no learner mutation. | Yes; stale change returns to preflight |
| `executing` | Execute-permitted server transaction owner | Atomic compare-and-set/row-version lock; duplicate requests return existing status. | Not by browser retry |
| `locally_committed` | Local transaction committed | Preserve safe result, clear target link as designed; never routine-rollback by restoring data. | No |
| `verification_pending` | Commit complete | Run safe verification; support stays `processing`. | Yes, verification retry |
| `backup_reconciliation_pending` | Verification passes but backup boundary remains | Record safe pending state; no `resolved`. | Yes |
| `completed` | Verification and approved backup completion rule pass | Write minimal evidence; only then allow support `resolved`. | No |
| `failed_manual_review` | Unexpected/local/provider/verification failure | Store safe failure code, retain/reconcile target link only as approved; support remains `processing`. | Manual recovery only |

## 7. Preflight specification

Algorithm: in an `AsNoTracking` read-only scope, load report by route ID; require category `account_deletion`; resolve user internally; resolve current Admin actor; reject/flag a target that is active Admin, sole Admin, or actor; then aggregate counts/booleans without selecting content. Inspect `users`, `user_profiles`, `user_settings`, refresh/reset tokens, devices, sessions/messages/feedback/summaries, usage counters/events/free use/trials, subscriptions/payments/entitlements, billing events/webhooks, feedback reports/replies, Admin actions/auth/role records, CMS author/audit references, and provider-linked records described by the canonical procedure.

For subscriptions, billing events and lifecycle-derived safe metadata, inspect every billing provider linked to the account through the shared backend subscription/entitlement model. Report only safe provider keys, counts, and flags for active renewal, pending cancellation, refund, chargeback, dispute, provider linkage, and any legal-hold indicator actually represented. An unknown provider, unsupported/missing adapter, unresolved purchase lifecycle, pending refund/chargeback/dispute, active renewal, or unverifiable provider state is `unclassified`/blocking, never `clear`. Resolve every category against the reviewed policy snapshot. Also report duplicate/prior operations and backup-policy readiness. The read-only first slice must not contact Paddle, Google Play, Apple, or another provider.

Normalize category keys/counts, state, policy version, and relevant row-version/timestamps into the fingerprint. A preflight expires after a future approved short TTL and becomes stale if report/user/subscription/operation relevant change tokens differ or the fingerprint recomputes differently. Unknown rows, unclassified provider/billing/audit dependencies, missing legal policy, or unavailable backup readiness block execution. It never loads/returns message text, reason, email, provider IDs, token values, or raw payloads and performs no token, status, or provider mutation.

## 8. Database and migration plan

Future migration sequence, without SQL:

1. Add operation/policy snapshot tables and `DbSet` mappings with bounded enums, JSON validation/max lengths, row-version column, safe check constraints, and active-operation uniqueness.
2. Add nullable, `Restrict` active FKs to report/target user/Admin actors. Do not change existing `users` relationships in the first slice.
3. Add completion-evidence/restore-manifest tables only when the destructive design is approved; its FK design must allow target link clearing without preserving a reversible mapping.
4. Update snapshot and add focused migration/model tests in the implementing change; deploy only under a separately approved migration plan.

Current `Restrict` dependencies are intentional blockers: support reports/replies, Admin actions, Admin users/roles/history, Admin auth audit, CMS authorship/audit, subscriptions/payments/entitlements, and current Paddle webhook rows cannot be made safe by cascades. The future destructive migration must explicitly choose each row's deletion/redaction/restricted-retention handling and may need nullable FKs or a safe detached evidence design. It must preserve historic CMS content validity, never cascade through other learners, and never silently erase required Admin/audit evidence. Future Google Play and Apple lifecycle storage must follow the same provider-neutral rule and must not add provider-specific columns to the anonymization operation model.

Rollback before destructive execution means rolling back the new foundation migration only through the reviewed migration-remediation process. After any committed anonymization, do not use database rollback/backup restore to return personal data; remediation is operation-state reconciliation and reapplication after restore.

## 9. Local transaction design

Later execution sequence:

1. Before the local transaction: fresh permission/actor/preflight/retention/billing/Admin checks; acquire operation lock; perform separately approved external prerequisites.
2. Inside one local transaction: compare fingerprint and row version; lock/invalidate access as designed; ordered child deletion/redaction (lesson messages/results/summaries, sessions, usage/devices/settings/profile/trials, entitlement and approved billing/support/audit handling); write safe state/evidence; clear the temporary target link; commit.
3. Outside transaction: reconcile provider actions, run verification, notification, and backup reconciliation. Each has durable safe state and idempotent retry.

If external cancellation succeeds but local commit fails, record only safe pending reconciliation and do not claim completion. If local commit succeeds while external action remains pending, stay awaiting external/verification and do not resolve support. Notification failure never restores personal data; retry separately according to approved policy. Verification failure after commit enters manual review; restore is not normal recovery. Process restart resumes from durable state and idempotency/row-version checks, never from browser memory.

## 10. Billing and external-provider boundary

First slice makes **no** provider calls. Later destructive execution uses a future provider-neutral billing adapter registry, not a Paddle-specific anonymization component. Each adapter (`paddle`, `google_play`, `apple_app_store`, or a future approved key) is responsible for reading current provider subscription state; detecting active renewal and pending cancellation, refund, chargeback, dispute, or another provider-specific blocker; performing or confirming an approved cancellation/account-closure action; returning a normalized safe result; supporting idempotent reconciliation; and receiving provider server notifications/lifecycle events through the existing billing subsystem. This design does not specify provider API calls.

The safer initial external model is operator-confirmed completion recorded as safe normalized state only, after owner/legal/provider approval; destructive execution stops when an adapter is unsupported, provider state is unverifiable, or a financial/retention decision is unresolved. `ExternalStatesJson` must remain provider-neutral and contain only provider key, action type, state, safe result code, timestamps, and manual-review indicator. It must never receive purchase tokens, receipts, personally linkable original transaction/customer/subscription IDs, webhook or notification payloads, signatures, credentials, or secrets.

### Future Mobile store compatibility

Google Play Billing purchase tokens must later be verified by the backend through the approved Google Play billing integration before any entitlement change. Apple StoreKit/App Store transactions and App Store Server Notifications must likewise be verified and normalized by the backend before any entitlement change. Google Play and Apple App Store results update the same internal subscription/entitlement source of truth used by Paddle and desktop; Mobile clients never grant Premium locally. Account anonymization operates on that normalized backend state and delegates only provider-specific external work to the matching adapter. Adding a mobile provider must not require changing the core anonymization state machine or operation schema; Google Pay and Apple Pay are not separate subscription-provider adapters.

Existing Paddle cancellation/refund/chargeback services are references, not authorization to reuse/call them. Do not infer retention periods or deletion guarantees from any provider.

## 11. Minimal completion evidence

Retain only operation ID; procedure/policy versions; timestamps; actor/approver safe references; state/result/verification/failure codes; category counts; provider-action state; and backup-reconciliation state. Prohibit email/display name, report reason/reply, lesson/support text, raw payload, provider identifiers, password/hash, tokens, typed confirmation, target user ID, and reversible mapping.

The support report may be linked only while active processing; on completion it must be deleted/redacted/detached or retained under the approved legal decision without making the evidence reversible. `admin_actions` cannot be the completion evidence record without schema/design change because it requires a target user and free-text reason/metadata. Restore manifests contain operation/evidence IDs and safe result state only.

## 12. Automated test plan

Future focused tests: route authorization and catalog mapping; no mutation during preflight; safe response serialization; stale fingerprint and typed-confirmation rejection; idempotency/duplicate concurrency; active Admin/self-target refusal; unresolved retention and billing blocker; FK-safe order; token/reset invalidation; unaffected second learner; external partial/verification/notification failures; backup reconciliation; and assertions that logs/evidence/responses contain no secrets or personal content. Add migration/model tests and service/endpoint tests in the implementation slice; do not add them in this documentation task.

## 13. Implementation slicing

**Slice 1 — read-only operation foundation:** add the two dedicated permission/policy/catalog mappings; operation and policy/preflight schema foundation plus migration; read-only preflight service/endpoint; operation-status read endpoint; and focused authorization/preflight/safe-response/staleness tests. An Admin UI may display preflight only under separate approval, with no destructive button. This slice must not alter learner data, revoke tokens, cancel subscriptions, call providers, notify, or resolve support.

The locally implemented browser confirmation, transaction, and verification flow are complete. A separately authorized combined migration/backend `.130` deployment remains pending; provider reconciliation, notification automation, and custom backup reconciliation are not part of this product process.

## 14. Open decisions and approval gate

**Before Slice 1:** approve exact permission split/role mapping; operation/preflight persistence model and safe response contract; policy snapshot source/versioning; whether read-only preflight can create/refresh a durable operation record; Admin CSRF/re-auth design boundary; and migration review/rollback plan.

**Before destructive execution:** legal entity/jurisdictions and legally required retention source/period for every retained category; hard deletion versus inert shell; safe evidence and current FK remediation; recent re-authentication mechanism; two-person approval; active/sole Admin process; billing/external action owner and provider obligations; financial/refund/chargeback/legal-claim treatment; backup completion rule/restore reconciliation; and notification-failure behavior. Provider-specific decisions include Google Play account linking and purchase-token verification, Google Play Real-time Developer Notifications, Apple transaction verification, App Store Server Notifications, renewals/cancellations/refunds/revocations/chargebacks/disputes/grace periods, provider-specific retention requirements, sandbox/test versus production separation, and ownership of external account-closure actions.

## Reviewed repository basis

Reviewed: canonical account-anonymization and account-deletion docs, `NEXT_STEPS.md`, command/backup/restore/billing/Paddle/Admin-RBAC/audit documentation; `ApiConstants`, Admin authorization/permission constants, Admin feedback endpoints, `AdminEndpoints`, `UserFeedbackReportEndpoints`, `Program`, persistent role catalog/actor resolver/audit services, account-deletion and feedback-status services, auth reset/refresh interfaces, billing cancellation/reconciliation services, EF context/entities/migrations/model snapshot, relevant tests, and history through `a1205f5a`.
