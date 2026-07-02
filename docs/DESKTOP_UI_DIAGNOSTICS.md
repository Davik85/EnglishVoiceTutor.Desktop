# Desktop UI diagnostics notes

## Lesson chat header indicators

The lesson chat header intentionally shows only three small status dots to regular users. It must not expose tooltip/help text, provider names, model IDs, or technical status wording in the normal desktop UI.

Maintainer-only meaning from left to right:

1. Tutor response state: ready vs. currently generating/responding.
2. Lesson history sync state: not started/active/finished/unavailable for local/backend lesson history persistence.
3. AI backend configuration reachability: checking/configured/unavailable from the backend configuration health check.

Model names and provider details are internal diagnostics only. Keep them in logs/admin diagnostics when needed, but do not show them to regular learners in the desktop lesson chat header.
