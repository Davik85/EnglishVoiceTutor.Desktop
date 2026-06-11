# Desktop backend route smoke check

The installed desktop app must build these absolute production URLs when the backend base URL is `https://api.languagevoicetutor.com`:

| Desktop request | Method | Route | Required production URL |
| --- | --- | --- | --- |
| Backend health | GET | `/health` | `https://api.languagevoicetutor.com/health` |
| Database health | GET | `/api/health/database` | `https://api.languagevoicetutor.com/api/health/database` |
| Register | POST | `/api/auth/register` | `https://api.languagevoicetutor.com/api/auth/register` |
| Login | POST | `/api/auth/login` | `https://api.languagevoicetutor.com/api/auth/login` |
| Session restore / me | GET | `/api/auth/me` | `https://api.languagevoicetutor.com/api/auth/me` |
| Authenticated cloud settings | GET/PUT | `/api/me/settings` | `https://api.languagevoicetutor.com/api/me/settings` |
| Authenticated subscription status | GET | `/api/me/subscription-status` | `https://api.languagevoicetutor.com/api/me/subscription-status` |

Repository backend route mappings currently expose `/health`, `/api/health`, `/api/health/database`, `/api/auth/register`, `/api/auth/login`, `/api/auth/me`, `/api/me/settings`, and `/api/me/subscription-status`. Production deployment must still be validated separately because repository routes do not prove what is deployed.

Run this smoke check against the same production base URL used by the packaged desktop app:

```powershell
pwsh ./tools/smoke_desktop_backend_routes.ps1 -BackendBaseUrl "https://api.languagevoicetutor.com"
```

To validate registration and `/api/auth/me`, pass a disposable tester account:

```powershell
pwsh ./tools/smoke_desktop_backend_routes.ps1 -BackendBaseUrl "https://api.languagevoicetutor.com" -Email "tester+desktop-smoke@example.com" -Password "Use-A-Disposable-Strong-Password-123!"
```

Missing optional routes such as cloud settings or subscription status must remain diagnostics only for the tester flow. They must not mark backend health unavailable, must not overwrite registration/login success, and must not block lessons, local settings, history, or progress.
