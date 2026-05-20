# Data Retention and Storage Policy (Draft)

## Purpose

This document defines our current product and technical policy draft for lesson data retention and storage in EnglishVoiceTutor.

It explains:
- what data is stored
- why it is stored
- whether data is long-term or temporary
- how storage supports desktop now and future mobile sync
- how storage supports progress tracking, feedback, summaries, usage limits, and product debugging

This is a product/technical policy draft, not a final legal policy.

## Storage Categories

### A) Long-term product records

The following records are core product data and can be kept long-term because they are needed for account continuity, cross-device sync, billing continuity, and learning history structure:

- **users**  
  Core account identity and ownership anchor for all user data.

- **user_profiles**  
  Learner profile fields that support personalization and continuity across desktop and future mobile.

- **user_settings**  
  User preferences (for example study settings) that should persist across sessions/devices.

- **lesson_sessions**  
  Session-level learning history (start/end/state/timestamps) needed for progress timelines and lesson lifecycle tracking.

- **lesson_summaries**  
  Compact long-term learning artifacts that represent durable progress and outcomes without requiring full raw message history.

- **subscriptions**  
  Entitlement records needed to enforce plan limits and preserve account access state.

- **payments**  
  Financial reconciliation and support records for billing operations.

- **devices**  
  Device linkage metadata for account/session management and future multi-device controls.

### B) Detailed learning history

The following records are valuable for learning quality but should not automatically be assumed to be permanent forever:

- **lesson_messages**  
  Useful for restoring full lesson context, generating/improving feedback, debugging user-reported issues, and personalization tuning.

- **feedback_results**  
  Useful for progress analysis, repeated mistake detection, and coaching quality over time.

Potential future retention controls:
- fixed windows (for example 30/90/180 days)
- user-controlled deletion behavior

### C) Usage and cost records

The following records are needed for limits, abuse prevention, cost control, and analytics:

- **usage_events**  
  Detailed event-level usage for enforcement and cost diagnostics. Over time, these may be archived or summarized.

- **daily_usage_counters**  
  Aggregated per-day counters for fast limit checks and trend analysis; can be kept long-term or further aggregated.

Guideline: usage data should avoid storing raw sensitive lesson text unless there is a clear necessity.

### D) Data we should not store

By default, we should avoid storing the following:

- raw audio files
- OpenAI API keys in desktop code
- connection strings/passwords in source control
- unnecessary full provider payloads (unless required for payment reconciliation)
- secrets in logs
- separate spoken-only bot text when visible text is different

Project rule: **Bot spoken text should match visible text.**

## Recommended MVP Policy

For MVP, keep policy practical and simple:

- Store **lesson_sessions**.
- Store **lesson_messages** for now to validate lesson history behavior, feedback quality, and future summary quality.
- Store **lesson_summaries** once summary persistence is implemented.
- Do **not** store raw audio.
- Do **not** store unnecessary provider secrets.
- Keep architecture simple until auth/subscriptions runtime logic is implemented.

## Recommended Future Policy

As product maturity grows:

- **lesson_summaries** should become the main long-term learning artifact.
- **lesson_messages** should be treated as detailed history with retention controls.
- Candidate retention policy options:
  - free users: keep detailed messages for 30 days
  - paid users: keep detailed messages for 90/180 days or longer
  - summaries: keep long-term
  - usage counters: keep long-term or aggregate
- Users should later be able to delete lesson history and account data.
- Desktop and mobile clients should sync through backend API contracts, not direct database access.

## Privacy and Safety Principles

- Data minimization: store only what is needed.
- Purpose limitation: each stored field should have clear product value.
- Plan for user deletion/export capabilities later.
- No secrets in source control.
- No raw audio storage by default.
- Avoid storing more than needed for learning and product quality.
- Do not expose stack traces or secrets in API responses.
- Backend owns PostgreSQL access; desktop clients never connect directly to PostgreSQL.

## Current Implementation Status

### Implemented

- users
- user_profiles
- user_settings
- lesson_sessions
- lesson_messages
- health endpoints
- desktop diagnostics
- desktop study language sync
- desktop lesson session tracking
- desktop lesson message persistence

### Not implemented yet

- lesson_summaries persistence
- feedback_results persistence
- usage_events persistence
- daily_usage_counters runtime logic
- auth/JWT
- payment/subscription runtime logic
- mobile sync
- user data deletion/export

## Next Recommended Persistence Step

The next logical persistence step is to implement **lesson summary persistence**.

Reason:
- summaries are the most useful long-term learning artifact
- they reduce dependence on keeping raw message history forever
- they can power user progress/history screens in both desktop and future mobile experiences

## Scope Note

This document is intentionally a product/technical policy draft to guide implementation decisions. It is not a final legal/privacy notice and does not claim formal GDPR/152-FZ compliance at this stage.


## Implementation status note

Backend lesson summary endpoints now exist for `lesson_summaries`, but the desktop summary flow is not connected yet.
