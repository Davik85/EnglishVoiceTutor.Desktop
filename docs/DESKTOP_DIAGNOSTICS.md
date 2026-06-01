# Desktop diagnostics

Review date: 2026-06-01.

The Windows desktop app Settings / Diagnostics panel checks the configured backend health endpoints when the user selects **Refresh diagnostics**.

## Release visibility

- Packaged Release builds hide Diagnostics by default.
- Diagnostics can appear in Release only when `EVT_DESKTOP_DIAGNOSTICS=1` is set locally before launching the app.
- Do not commit `EVT_DESKTOP_DIAGNOSTICS` in scripts, settings files, shortcuts, docs with machine-specific values, or tester package configuration.
- Debug builds keep Diagnostics visible for development.

## Health checks

- Backend health is checked with `GET /api/health` on the configured Backend URL.
- Database health is checked with `GET /api/health/database` on the configured Backend URL.
- Backend AI configuration status is checked with `GET /api/backend/config-status`.
- The desktop app reports Backend status as Healthy only when the backend health endpoint returns a healthy response.
- The desktop app reports Database status as Healthy only when the database health endpoint returns a healthy response and `canConnect` is `true`.

## Local requirements

- The backend must be running locally, for example at `http://localhost:5000`, for Backend status to be Healthy.
- PostgreSQL must be reachable by the backend for Database status to be Healthy.
- If the backend is stopped or unavailable, diagnostics should show the backend and database as unavailable and the desktop app should not crash.

## Safety

Diagnostics does not expose secrets. The copied diagnostics report includes backend and database status, database provider when available, and short safe database error text when the database is unavailable. It must not include connection strings, OpenAI API keys, JWTs, billing data, webhook secrets, provider keys, environment variables, lesson messages, raw audio file paths, lesson history content, or other secrets.

`OPENAI_API_KEY` is backend-only and must never be added to desktop or sent to testers. Desktop only needs a Backend URL and must call backend APIs only.
