# Account anonymization procedure and data inventory

> **Current approved local procedure (2026-07-23).** This section supersedes earlier draft workflow, retention, external-provider, confirmation, notification, and restore-reconciliation proposals below. A Super Admin may use the locally implemented Admin Shell confirmation dialog to execute a fresh, matching preflight for a `processing` account-deletion report. No second Admin, password re-entry, typed phrase, provider call, queue, email automation, or custom backup workflow is required. Active effective Premium or a current paid subscription period blocks execution until expiry; the operator reminds the customer to cancel renewal. Refunds, disputes, and chargebacks remain manual support work. The transaction deletes normal learner data and access, retains unchanged local subscription/payment/billing/Paddle records, redacts support content, and leaves a non-login `deleted+<operationId>@deleted.invalid` user shell. Standard backup retention applies. The combined migration and backend deployment remain pending; production remains `0.1.35-backend.129`.

## 1. Status and scope

**Status: complete local implementation; deployment remains subject to separately authorized operational review.** Production backend `0.1.35-backend.129` has learner request intake and the non-destructive Slice 1 preflight foundation. The repository also implements, but has not deployed, Super-Admin email-request intake and the complete Admin Shell confirmation/execution flow. The combined migration and backend deployment remain pending.

Admin email intake requires only a short operator comment and creates the normal support request; it has no second-Admin approval or two-person workflow. It does not call or modify Paddle or another provider. An active Premium period will block future actual deletion until the paid period ends, so the operator should remind the customer to cancel renewal; refunds, disputes, and chargebacks remain manual support matters.

For the future backend implementation companion and its explicitly read-only first slice, see [Account anonymization backend technical design](ACCOUNT_ANONYMIZATION_BACKEND_DESIGN.md).

This procedure is intended to make anonymization irreversible. It does not approve a retention period, claim legal compliance, or authorize an operator to run mutations. Support must not mark a request `resolved` until an approved complete operation and its verification have succeeded. Exact statutory retention periods, the controller/legal entity, and applicable jurisdictions require owner/legal approval and are deliberately not invented here.

The scope is the repository-controlled live application, its documented providers, logs, and backup/restore path. It excludes production-data inspection and does not make a claim about storage not represented by the reviewed repository or runbooks.

## 2. Definitions

| Term | Product meaning |
| --- | --- |
| Deletion | Removal of a record or data value so it is no longer retained in the live system; it is not synonymous with replacing an email. |
| Anonymization | Irreversible processing so the person is no longer reasonably identifiable or linkable from the retained data, including by IDs, free text, provider identifiers, or retained mappings. |
| Pseudonymization | Replacing an identifier while a mapping, join path, stable identifier, or other realistic re-linking route remains. It is not complete anonymization. |
| Restricted retention | Keeping the minimum necessary record in a separately controlled access domain only where applicable law requires it or it is necessary for the establishment, exercise, or defence of legal claims. It is not a general-purpose archive. |
| Non-linkable aggregate | A population-level value retained only after approved aggregation/minimization that cannot reasonably be joined back to the learner. |
| Data recipient / external processor | A separate party or system receiving data for the product, including Paddle, SMTP delivery infrastructure, and OpenAI processing. |
| Live system | The active application database, deployed service, active provider records, and operational logs usable in normal operations. |
| Backup system | Database backups, restore copies, snapshots, and drill copies that can later reintroduce historic data. |

## 3. Complete data inventory

`AppDbContext` is the schema source for the database rows below. A GUID, hash, provider ID, metadata JSON, transcript, report text, or raw payload is treated as potentially personal unless an approved assessment proves otherwise. `Restrict` foreign keys mean the current schema cannot hard-delete `users` without ordered changes; that is an implementation constraint, not approval to retain data.

### Approved retention principle and decision record

**Language Voice Tutor retains personal data after an approved anonymization request only when retention is required by applicable law or necessary for the establishment, exercise, or defence of legal claims. Each retained category requires a documented legal basis and retention period. Access is restricted to the approved purpose. When the mandatory period expires, the data must be deleted or irreversibly anonymized.**

Optional personal data must not be retained merely because a longer period might be legally permitted. For every proposed retained category, the future implementation record must name the legal source, mandatory period, approved purpose, access restriction, and destruction/anonymization event at expiry. The period must be determined separately from the operating legal entity, its country of establishment, relevant learner/customer jurisdiction, tax/accounting duties, payment/refund/chargeback/fraud/legal-claim duties, and external-provider contracts. No period is selected in this document.

| Decision | When allowed | Required outcome |
| --- | --- | --- |
| Immediate deletion or irreversible anonymization | The data is not required by law and is not necessary for a legal claim. | Remove it during the approved operation; do not preserve a reversible mapping. |
| Legally required restricted retention | A documented law requires the category, or it is necessary for the establishment, exercise, or defence of a legal claim. | Retain only the necessary minimum, restrict access and secondary use to that purpose, record legal source and mandatory period, then delete or irreversibly anonymize at expiry. |
| Unresolved legal decision | The legal source, period, or necessity is not yet documented. | Do not treat the request as complete; escalate to owner/legal rather than retaining by default. |

| Table/system | Linkage and personal/raw-data risk | FK/dependency | Proposed action and reason | Legal decision? | Verification / backup consideration |
| --- | --- | --- | --- | --- | --- |
| `users` | `Id`; email, password hash, status | Principal for most learner rows; referenced by Admin/CMS audit paths | **Irreversibly overwrite or delete only after dependents.** Remove login identity; no retained email, password hash, or reversible map. | Yes, final hard-delete versus inert shell | Login and direct email lookup fail; restore reconciliation must reapply. |
| `user_profiles` | `UserId`; display name, language, level, timezone | Restrict to `users` | **Delete.** Direct profile identifiers/preferences. | No, absent approved exception | No row; backup pending boundary. |
| `user_settings` | `UserId`; language/voice/conversation choices | Restrict to `users` | **Delete.** Linkable preferences. | No | No row. |
| `user_refresh_tokens` | `UserId`; token/replacement hashes, IPs, user agent | Restrict to `users` | **Delete after immediate revocation.** Token hashes and telemetry remain linkable/security-sensitive. | No | No usable refresh token or row. |
| `password_reset_tokens` | `UserId`; token hash | Restrict to `users` | **Delete after immediate invalidation.** | No | Reset cannot complete. |
| `devices` | `UserId`; device name/platform/version | Restrict to `users` | **Delete.** Device fingerprint risk. | No | No row. |
| `lesson_sessions` | `UserId`; topic/history, timing, cost | Parent of messages/results/summaries/usage/free usage | **Delete after children.** Learner history remains linkable. | Historical aggregate decision | No session/history lookup. |
| `lesson_messages` | Via `SessionId`; message text/transcripts, role, audio metadata | Restrict to session; referenced by feedback | **Delete.** Free text/transcripts cannot remain as linkable history. | No | No learner text/transcript remains. |
| `feedback_results` | Via session/message; feedback text/data | Restrict to session and message | **Delete.** Learner/AI-derived content is potentially personal. | No | No row. |
| `lesson_summaries` | Via session; summary free text | Restrict to session | **Delete.** Free text may identify the learner. | No | No row. |
| `usage_events` | `UserId`, optional session; operation/token/audio/cost metrics | Restrict to user/session | **Delete, or retain only approved non-linkable aggregate.** Row-level event remains linkable. | Yes for aggregate need | No per-user events; aggregate assessment recorded. |
| `daily_usage_counters` | `UserId`; daily behaviour/cost | Restrict to user | **Delete, or non-linkable aggregate only.** | Yes | No user/day counters. |
| `daily_free_lesson_usage` | `UserId`, `LessonSessionId`; usage timing/count | Restrict to user/session | **Delete.** Eligibility/behaviour is linkable. | No | No row. |
| `trial_grants` | `UserId`; source platform/status/timing | Restrict to user | **Delete.** Linkable entitlement history. | No | No row. |
| `entitlements` | `UserId`, subscription; plan/status/source | Restrict to user/subscription/plan | **Delete after disabling access.** No active access can remain. | No | Entitlement query returns none/inactive as approved. |
| `subscriptions` | `UserId`; Paddle customer/subscription/price/product/event IDs | Restrict to user/plan; payments/entitlements may depend | **Restricted retention or irreversibly minimize, pending billing matrix.** First stop renewal externally. Provider IDs are identifiers, not anonymous. | Yes | No active renewal; retained row fields match approval. |
| `payments` | `UserId`, subscription; amounts/status and provider payment/customer/subscription IDs | Restrict to user/subscription | **Restricted retention/minimize pending accounting, refund, and claim decision.** | Yes | Retained fields/counters match approved basis. |
| `billing_events` | Provider event ID/status, possibly provider-linked | No user FK visible; can be linkable through event/provider data | **Unresolved: classify by payload/normalization provenance; minimize or restrict if linkable.** | Yes | Approved linkage analysis and retained-field check. |
| `paddle_webhook_events` | Optional internal user ID; raw Paddle payload/signature metadata | Restrict to user where linked | **Restricted retention/minimize or delete pending legal/provider decision.** Raw payload can carry identity and financial data. | Yes | No raw payload beyond approved basis; provider action logged. |
| `user_feedback_reports` | `UserId`; deletion reason/message, client metadata, support status | Parent of replies; restrict to user | **Delete or irreversibly overwrite content; retain only minimum safe completion evidence in a future separate record.** | Yes for support/legal exception | No reason/free text; status alone is not proof. |
| `user_feedback_report_replies` | Report/Admin IDs; reply text, recipient email, delivery status | Restrict to report/admin user | **Delete or irreversibly overwrite recipient/reply text.** | Yes for support exception | No email/reply text. |
| `admin_actions` | `TargetUserId`; reason/metadata can be personal | Restrict to target and actor user | **Restricted retention or safe anonymized audit redesign.** Current target FK conflicts with deleting user. | Yes | No direct target identity or unsafe metadata; conflict decision recorded. |
| `admin_auth_audit_events` | Optional actor user/admin IDs; auth events/IP-related metadata may exist | Restrict to `users`/`admin_users` | **Restricted retention/minimize.** Needed only under approved security basis; active account must not be an anonymization target. | Yes | Safe audit reference/no learner identity as approved. |
| `admin_users` | Optional `UserId`; status/admin identity | Restrict to user and creator admin | **Unresolved/stop condition for active Admin.** Do not anonymize an active Admin identity; transfer/disable roles and approve separately. | Yes/security | No active role or self-operation; no FK break. |
| `admin_user_roles` | Admin identity, role/reason | Restrict to admin users | **Restricted retention/minimize, after active-admin decision.** | Yes/security | Roles revoked/handled under approved plan. |
| `admin_role_assignment_events` | Actor/target Admin IDs, reasons | Restrict to admin users | **Restricted retention/minimize.** Audit/history FK conflict requires design. | Yes/security | Safe retained audit reference. |
| CMS authorship: `cms_content_packs`, `cms_prompt_templates`, `cms_content_versions`, `cms_content_audit_logs` | Created/updated/published/actor `UserId`; audit reason/metadata may be free text | Restrict to `users`; content also has CMS dependencies | **Restricted retention with irreversibly safe actor representation, or approved deletion/redaction.** Do not alter published content merely to erase author linkage. | Yes/security | Content remains valid; no actor link/free text beyond approval. |
| CMS content tables: `cms_lesson_topics`, `cms_lesson_scenarios`, `cms_tutor_behavior_profiles`, `cms_published_content_snapshots` | No learner linkage found | CMS FKs only | **Not user-linked.** Leave unchanged unless a specific content value embeds personal data. | No | Unchanged and valid. |
| Reference tables: `lessons`, `plans` | No learner linkage found | Referenced by sessions/subscriptions/entitlements | **Not user-linked.** Leave unchanged. | No | Unchanged. |
| Application/server logs | Email/IP/request IDs, error text, provider identifiers and potentially request content depending on logging path | Outside EF; configured deployment/logging | **Restricted retention/minimize; expire under approved log policy.** Do not scrape or rewrite live logs manually. | Yes | Logging owner confirms scope/expiry and safe operation reference. |
| SMTP/email delivery | Recipient email, subject/body and delivery metadata | External recipient/processor; support and reset mail paths | **External-provider action required; minimize/delete where supported.** Future learner notification is a separate delivery attempt. | Yes | Provider/support evidence contains no message body or address in completion audit. |
| Paddle | Customer, subscription, payment/refund/chargeback/invoice records | External processor; IDs copied locally | **External-provider action required and legal decision.** Cancel renewal first; request/delete/minimize only according to approved Paddle and accounting duties. | Yes | Provider action status/reference only, never raw payload. |
| OpenAI lesson/chat/voice processing | Lesson text, transcripts/audio and transient request/response content can be sent | External processor; repository has no user-linked EF copy beyond lesson rows/usage | **External-provider retention/deletion decision required.** Do not assume API processing is anonymous. | Yes | Processor terms/configuration and approved action documented. |
| Backups, snapshots, restore/drill copies | Historic full database/log data | Backup runbooks, not application FKs | **Retain only within approved backup policy; place beyond normal use and reconcile on restore.** | Yes | Pending/complete backup boundary recorded without credentials. |

## 4. Proposed processing stages

The future operation must use a durable operation record/state machine; it is a future schema/API decision, not a new status in the present product. Every stage emits a safe operation ID, timestamp, actor/audit reference, result, and category counts—never original identity or content.

| Stage | Required input / actor / before | Action and after | Evidence, retry, and stop conditions |
| --- | --- | --- | --- |
| Eligibility review | Request ID; authorized support; `new` or `reviewed` | Check category, duplicate/terminal history, scope and requester eligibility; after: ready for identity check or `needs_information`/`rejected`. | Safe to reread. Stop on duplicate ambiguity, wrong category, or missing approval. |
| Identity and request verification | Authenticated/request-verification evidence; privileged operator | Verify via approved out-of-band procedure without copying password/reason; after: verified or halted. | Safe verification reference only. Stop on mismatch. |
| Legal-retention decision | Inventory and owner/legal decision | Apply approved matrix to every retention category; after: signed decision set. | Retry decision collection; stop if any required decision is absent. |
| Preflight/dry run | Verified target; service account with least privilege | Count every inventory category, detect FKs, active Admin, subscription/payment/refund/chargeback; after: immutable safe plan/counts. | Repeatable/no mutation. Stop on unclassified data or unexpected count. |
| Browser confirmation | Completed non-mutating preflight; strongest approved Admin role | Show the current preflight summary and collect acknowledgement/typed confirmation before any mutation request; after: confirmation may be submitted for a fresh server check. | Expire stale preflight/confirmation state; duplicate clicks are harmless. Stop on mismatched request/operation ID, stale data, missing acknowledgement, or incomplete typed confirmation. |
| Billing/provider review | Preflight and billing authority | Check renewal, refund, chargeback, invoice/legal hold; request required Paddle/SMTP/OpenAI actions; after: external work complete/pending. | Use provider action idempotency where available. Stop if future renewal or required retention conflict remains. |
| Access lock and credential revocation | Approved plan; privileged operator | Lock account, invalidate sessions, revoke refresh and reset tokens before content work; after: no new access. | Repeating is safe. Stop if lock/revocation cannot be proven. |
| Admin identity/role check | Preflight | Refuse active Admin, self-target, sole-admin, or unresolved role/audit dependency; after: transferred/disabled under separate approval or stopped. | Read-only retry. Stop on any active-admin condition. |
| Local database operation | Approved ordered plan, exclusive target lock | In one designed transaction, delete/redact child-to-parent categories and write only safe completion evidence; after: committed anonymization state. | Idempotent by operation ID. Stop/rollback before commit on FK/count/concurrency error. |
| Post-operation verification | Committed operation | Run approved read-only checks; after: verified or failed. | Repeatable. Stop ticket resolution on any failed check. |
| Backup/restore reconciliation | Backup policy and operation ID | Mark backup boundary/pending expiry; ensure future restore replays completed operations before availability; after: pending or complete accurately. | Repeatable. Stop final completion if approved policy is absent. |
| Learner notification | Verified operation and approved wording | Send only after local/external outcomes are known; after: delivery attempted/recorded safely. | Retry notification separately; notification failure does not reverse anonymization but blocks `resolved` unless approval says otherwise. |
| Support resolution | All blocking checks, evidence, approval | Mark existing request `resolved`; after: support queue reflects verified completion. | Idempotent status update. Stop if operation is partial, evidence missing, or backup state inaccurately represented. |

## 5. Request status rules

Current support statuses are `new`, `reviewed`, `needs_information`, `processing`, `resolved`, and `rejected`; the intake service treats the first four as active for duplicate prevention.

- `new` and `reviewed` permit read-only preparation and assignment. `needs_information` is required when identity, authority, or required decision/evidence is missing; processing cannot start.
- Start processing only from `processing`, after identity verification, legal/retention approval, dry run, billing/Admin checks, and operation authorization. Current generic status handling does not enforce those rules, so they are operational requirements.
- `rejected` is allowed only for a documented approved reason (for example, unverifiable request, authority/legal hold, or disallowed scope) and must not imply any data operation.
- `resolved` is allowed only after the complete operation, verification, required provider actions, and approved backup state. A status change alone never proves anonymization; the present status service only changes a support row and writes an Admin action.
- There is no current status for failure/partial completion. Until a future schema/API decision adds one, keep the request `processing`, attach no unsafe detail, record safe failure evidence in the approved operational record, and escalate. Do not invent or deploy a status value in this task.

## 6. Access and authorization design

Initially restrict any future destructive execution to `super_admin` with a dedicated permission, not the broad feedback-status permission. Recent re-authentication remains an approval decision; no second-Admin approval or two-person workflow is planned. The existing RBAC model and `admin_actions`/Admin-auth audit pattern support accountable actor records, but their current target-user FKs and free-text fields require a safe-audit implementation decision.

The design must reject self-targeting, active Admin identities, sole/last-admin cases, and duplicate operation IDs. An Admin user must first be transferred or disabled under a separately approved role/audit process; never silently remove a live privileged identity.

### Required future Admin CMS browser confirmation

The future Admin CMS must not offer a one-click destructive action and must not rely solely on `window.confirm()`. The non-mutating preflight is mandatory and must finish first. The browser then opens a modal that shows the irreversible consequence and the current preflight summary of data categories to be deleted, irreversibly anonymized, retained under the documented legal rule, or requiring an external-provider action.

Before it enables the explicitly destructive final button, the modal must require both a mandatory acknowledgement checkbox and manually typed confirmation tied to the account-deletion request ID or operation ID. No mutation request may be sent until those requirements are complete.

Browser confirmation is an accidental-click safeguard, not the authorization boundary. On final submission, the server must perform a fresh authorization, recent-authentication/two-person-approval check if approved, request-state check, target/operation-ID match, and current preflight/concurrency check before it accepts any mutation. The design must safely reject stale pages, expired or changed preflight results, duplicate clicks, repeated submissions, and an operation already completed or in progress. The UI must show a safe retry/result state rather than treating a browser action as proof of completion.

## 6.1 Future Mobile placement and warning requirements

The existing Mobile account-deletion request feature remains implemented. Its future location is intended to move to **Settings → App → Contact & Support → Request account deletion**; the final section label must follow the actual Mobile Settings structure when the Mobile implementation is designed.

Before current-password entry and submission, the future screen must neutrally explain that the account contains lesson history, progress, achievements, settings, and other saved learner information; deletion/anonymization is irreversible; affected information will no longer be available after completion; the learner should continue only if they intend to permanently close the account; and submission creates a support-managed request rather than immediate deletion. Current-password confirmation remains required and the reason remains optional.

This neutral information must not become an obstruction: it must not demand a “good” or “serious” reason, shame, pressure, frighten, hide the request behind unrelated actions, promise export before it exists, or make an unverified absolute security claim. Product-safe wording may say that Language Voice Tutor applies measures to protect account data, rather than guaranteeing that data can never be lost or accessed.

## 7. Transaction, idempotency, and failure recovery

1. Dry-run row counts and dependency graph are read-only and bound to the operation ID.
2. Order mutations deterministically: lock/revoke credentials; settle entitlement/provider gates; delete lesson children; delete sessions/usage/device/settings/profile/trial rows; apply approved billing/support/audit retention transformations; finally delete or irreversibly overwrite the user only when every FK decision permits it.
3. The local mutation and safe completion record need a clearly defined transaction boundary. An external provider call cannot share that transaction: persist a safe pending/outbox state before it, use provider idempotency where available, and reconcile rather than guessing.
4. A unique operation ID plus target-level concurrency lock must make a repeat return the already-recorded result or continue only the documented incomplete stage; it must never create a second audit action or reprocess another learner.
5. If an external action succeeds and local commit fails, retain only a safe pending/reconciliation reference, do not claim completion, and reconcile before retry. If local anonymization commits but notification fails, do not restore personal data; retry notification through a separate safe delivery workflow and keep the ticket unresolved if policy requires notification.
6. A normal database rollback after a committed operation, or restoring an older backup, can reintroduce personal data. It is not a routine per-request rollback mechanism. All failures that leave an unverified, partial, provider-pending, FK-invalid, or backup-unreconciled state block ticket resolution.

## 8. Retained audit evidence

The future completion record should contain only: anonymization operation ID; account-deletion request ID; procedure/version ID; timestamps; executing Admin ID or safe audit reference; result; per-category counts; external-provider action statuses; backup-reconciliation status; and safe failure codes.

It must not contain the original email/display name, request reason, lesson text, transcript, support reply, provider raw payload, tokens, password/hash, or reversible identity map. Current `admin_actions` (target FK, reason and metadata), support rows, CMS audit rows, and Admin audit rows may conflict with this rule. The implementation must explicitly decide whether to create a purpose-built minimal evidence store, safely detach/redact FKs, or retain a restricted audit record; this procedure does not choose one.

## 9. Billing and financial retention decision matrix

| Category | Proposed treatment | Approval / recipient requirement |
| --- | --- | --- |
| Future renewal/cancellation | Prevent future renewal before local mutation; remove live Premium entitlement. | Paddle action and billing-owner approval required. |
| Premium/entitlement | Immediate deletion after access is disabled and financial gates pass. | No retention period assumed. |
| Provider customer/subscription IDs | Immediate deletion/anonymization unless documented legally required restricted retention applies. | Paddle/accounting/legal decision must identify legal source and mandatory period. |
| Successful/failed payments | Legally required restricted retention only when the documented accounting/claim basis requires it; otherwise delete/anonymize. | Owner/legal/accounting approval must identify legal source and mandatory period. |
| Refunds/chargebacks | Restricted retention only for the documented mandatory process/claim period; otherwise delete/anonymize. | Owner/legal/accounting approval must identify legal source and mandatory period. |
| Invoices/accounting evidence | Restricted retention only if required; delete or irreversibly anonymize at the mandatory period’s end. | Legal entity/jurisdiction decision and legal source required; Paddle may be recipient. |
| Paddle raw webhook payload | Immediate deletion or minimum safe transformation unless a specific documented mandatory basis requires restricted retention. | Paddle/legal/security decision must identify legal source and mandatory period. |
| Legal claims | Restricted retention only while necessary for the documented claim purpose; delete/anonymize at expiry. | Legal approval must identify legal source and mandatory period. |

## 10. Backup and restore procedure

Process live data through the future approved operation only. Existing backups and restore/drill copies must be placed beyond normal operational use; they expire or are overwritten only under an approved backup policy, never by ad-hoc deletion. Record a safe backup boundary (operation ID, backup policy/version, oldest relevant backup boundary, and `pending`/`complete` reconciliation state), not backup credentials, paths, connection strings, commands, or contents.

Before any restored environment becomes available, the restore operator must identify completed anonymization operations after the backup point and reapply the approved operation/reconciliation set before user access. The backup/restore owner verifies this in every restore drill and records the safe result. A support request cannot be represented as fully complete without an approved backup policy and accurate pending/completed backup state.

## 11. Future implementation verification checklist

- Login is denied; access/refresh tokens are unusable; password reset cannot succeed.
- No direct email or display-name lookup finds the learner; profile/settings/devices are deleted or sanitized as approved.
- Lesson free text, transcripts, feedback, summaries, and user-linked history are removed as approved.
- No active entitlement or renewal remains; retained billing/audit fields match the approved matrix.
- Support content is deleted/minimized as approved; provider actions and backup state are safely recorded.
- Foreign keys remain valid; repeated execution is safe; another learner is unaffected.
- Aggregates are non-linkable under the approved re-identification assessment.
- Active Admin accounts cannot be accidentally anonymized.

## 12. Open approval decisions

1. Legal entity, controller role, applicable jurisdiction, and exact statutory retention periods.
2. Accounting/tax, payment, refund, chargeback, invoice, and legal-claim retention duties.
3. Whether the product promise is hard deletion, anonymization, or a defined combination.
4. Whether any historical lesson/usage aggregates are needed and what proves they are non-linkable.
5. Whether all lesson/support free text, transcripts, summaries, and feedback are deleted entirely.
6. Paddle, SMTP, OpenAI, and any infrastructure processor deletion/retention duties and the authorized contact/action path.
7. Backup expiry, immutable/snapshot handling, restore-reconciliation mechanism, and completion boundary.
8. Initial Admin permission, super-admin-only period, re-authentication, two-person approval, and active-Admin handling.
9. Safe evidence storage that resolves present Restrict-FK and audit-content conflicts.
10. Learner-notification wording, timing, delivery-failure rule, and rejection wording.
11. Whether logs or unidentified storage outside this repository contain user-linked data; if so, expand the inventory before implementation.

## Reviewed repository basis

Reviewed: `AppDbContext`, entity classes, migrations/model snapshot, account-deletion request service/tests and feedback status service, authentication token/reset paths, Admin RBAC/audit entities, billing/Paddle endpoint/service registration, SMTP/OpenAI registrations, `ACCOUNT_DELETION_REQUESTS.md`, `NEXT_STEPS.md`, retention/privacy/logging, backup/restore, billing/Paddle, and Admin audit documentation, plus history containing account-deletion, Admin audit, RBAC, and Paddle/refund decisions. The repository contains no pre-existing canonical anonymization procedure; this file is the canonical design document.
