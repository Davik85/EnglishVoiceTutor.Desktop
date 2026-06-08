# Backend Server Deployment Foundation

This document describes the prepared deployment foundation for the Language Voice Tutor backend on Ubuntu 24.04. It is a manual, test-deployment workflow. The current production-like backend has now been verified for the `0.1.8-tester.1` internal smoke baseline, but that does not make external tester handoff or public release ready.


## Current server verification for v0.1.8-tester.1

The production-like backend is reachable at `https://api.languagevoicetutor.com` for the `0.1.8-tester.1` internal smoke baseline. The backend health endpoint and database health endpoint have been verified healthy on the server, and PostgreSQL migrations have been applied on the server.

Do not copy server secrets, passwords, API keys, private keys, tokens, private environment values, private IP-sensitive credentials, or provider credentials into this document or any tracked file.

## Scope

- Static Windows installer hosting and backend API hosting are separate deployment tracks.
- The current static HTTPS site is already available at `languagevoicetutor.com` for public/static hosting work.
- The backend API is planned for `api.languagevoicetutor.com`.
- The backend remains the source of truth for AI/provider calls and account/subscription decisions.
- The desktop app must call backend APIs only and must never store OpenAI API keys directly.
- Production billing remains deferred. Paddle keys are not required for this test backend unless checkout/billing tests are deliberately enabled later.

## Server layout

Expected Ubuntu 24.04 paths:

```text
/opt/languagevoicetutor/backend
/opt/languagevoicetutor/backend/releases/{version}
/opt/languagevoicetutor/backend/current -> /opt/languagevoicetutor/backend/releases/{version}
/etc/languagevoicetutor/backend.env
/var/log/languagevoicetutor/backend
```

The service runs as the `deploy` user. PostgreSQL is expected to already be installed with database `lvt_app_db` and application user `lvt_app`.

## Backend configuration inspected

The backend reads `ConnectionStrings:DefaultConnection` during startup and fails fast if it is missing. The equivalent server environment variable is `ConnectionStrings__DefaultConnection`.

The backend validates `Jwt:SigningKey` during startup and requires at least 32 characters. The equivalent server environment variable is `Jwt__SigningKey`.

The OpenAI key is read only from the `OPENAI_API_KEY` environment variable. The backend's OpenAI model is read from `OpenAI:Model`, which can be overridden with `OpenAI__Model`.

The existing unauthenticated health endpoints are:

- `GET /health`
- `GET /api/health`
- `GET /api/health/database`

`/health` and `/api/health` return status, environment, and UTC timestamp without requiring OpenAI or exposing secrets. `/api/health/database` performs a safe database connectivity check without exposing the connection string.

CORS is not currently configured in the backend. Nginx should reverse-proxy API traffic to the localhost Kestrel listener.

## Local package command

Run from the repository root on the local Windows development machine:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.6-tester.1
```

This publishes `backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj` in Release mode for `linux-x64` as a self-contained deployment and writes:

```text
artifacts\publish\backend-linux-x64
artifacts\packages\backend\LanguageVoiceTutor.Backend-linux-x64-0.1.6-tester.1.zip
```

Generated files under `artifacts/` must not be committed.

## Upload dry-run

Dry-run validates the local archive and prints the SSH/SCP commands without uploading:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version 0.1.6-tester.1 `
  -ServerHost "api.languagevoicetutor.com" `
  -ServerUser "deploy" `
  -RemotePath "/opt/languagevoicetutor/backend" `
  -DryRun
```

Use real SSH host details only from the local operator environment. Do not commit IP addresses, SSH key paths, passwords, tokens, or secrets.

## Upload

After SSH access exists outside git, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version 0.1.6-tester.1 `
  -ServerHost "api.languagevoicetutor.com" `
  -ServerUser "deploy" `
  -RemotePath "/opt/languagevoicetutor/backend"
```

If the server uses a non-default SSH port, add `-SshPort 2222` with the actual port. The upload script creates a versioned release folder, unzips the archive, and updates `current`. It does not restart the service unless `-RestartService` is explicitly provided, and it never runs EF migrations.

## Server environment file

Create the real environment file on the server outside git:

```bash
sudo mkdir -p /etc/languagevoicetutor
sudo install -o root -g deploy -m 0640 /dev/null /etc/languagevoicetutor/backend.env
sudo nano /etc/languagevoicetutor/backend.env
```

Use `docs/server/backend.env.example` as the placeholder template. Replace placeholders with real values only on the server. Database passwords, OpenAI keys, Paddle keys, SMTP passwords, provider keys, JWT signing keys, SSH keys, and API keys must never be committed.

At minimum for the test backend, provide:

```text
ASPNETCORE_ENVIRONMENT=Production
DOTNET_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5001
ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=lvt_app_db;Username=lvt_app;Password=<database-password>
Jwt__SigningKey=<long-random-secret-at-least-32-characters>
OPENAI_API_KEY=<backend-only-openai-key>
SubscriptionEnforcement__Enabled=false
Billing__CheckoutEnabled=false
Billing__Provider=none
PaddleBilling__CheckoutAdapterEnabled=false
PaddleWebhook__Enabled=false
```

## systemd service installation

Review `docs/server/languagevoicetutor-backend.service.example`, then install it on the server:

```bash
sudo cp languagevoicetutor-backend.service.example /etc/systemd/system/languagevoicetutor-backend.service
sudo systemctl daemon-reload
sudo systemctl enable languagevoicetutor-backend.service
sudo systemctl start languagevoicetutor-backend.service
sudo systemctl status languagevoicetutor-backend.service --no-pager
journalctl -u languagevoicetutor-backend.service -n 100 --no-pager
```

The example unit uses:

```text
WorkingDirectory=/opt/languagevoicetutor/backend/current
EnvironmentFile=/etc/languagevoicetutor/backend.env
ExecStart=/opt/languagevoicetutor/backend/current/EnglishVoiceTutor.Api
User=deploy
```

Kestrel must bind only to `http://127.0.0.1:5001` through `ASPNETCORE_URLS`. Do not expose Kestrel directly to the internet.

## nginx API reverse proxy

Review `docs/server/nginx-api-languagevoicetutor.example`, then install a separate nginx site for the API. Keep the existing static `languagevoicetutor.com` site config separate.

Example server commands:

```bash
sudo cp nginx-api-languagevoicetutor.example /etc/nginx/sites-available/api.languagevoicetutor.com
sudo ln -s /etc/nginx/sites-available/api.languagevoicetutor.com /etc/nginx/sites-enabled/api.languagevoicetutor.com
sudo nginx -t
sudo systemctl reload nginx
```

The API proxy should forward to:

```text
http://127.0.0.1:5001
```

The example intentionally does not hardcode certificate paths because Certbot should manage TLS changes later.

## Certbot for the API domain

After DNS for `api.languagevoicetutor.com` points to the VPS and nginx serves the HTTP site, request TLS with Certbot:

```bash
sudo certbot --nginx -d api.languagevoicetutor.com
sudo nginx -t
sudo systemctl reload nginx
```

Certbot may modify the nginx config for HTTPS redirects and certificate paths.

## Health checks

From the server:

```bash
curl -fsS http://127.0.0.1:5001/health
curl -fsS http://127.0.0.1:5001/api/health/database
```

From a client after nginx and TLS are configured:

```bash
curl -fsS https://api.languagevoicetutor.com/health
curl -fsS https://api.languagevoicetutor.com/api/health/database
```

## EF migrations

No EF migration is needed for this deployment foundation. No database schema change is required for the existing health endpoints or the package/upload/service documentation.

Do not run migrations automatically from the upload script. Apply EF migrations deliberately and explicitly only after reviewing the target database, backups, environment, and migration list.

## Rollback idea

Each upload extracts to a versioned folder under:

```text
/opt/languagevoicetutor/backend/releases/{version}
```

To roll back manually, point `current` to a previous release and restart the service:

```bash
sudo ln -sfn /opt/languagevoicetutor/backend/releases/<previous-version> /opt/languagevoicetutor/backend/current
sudo systemctl restart languagevoicetutor-backend.service
sudo systemctl status languagevoicetutor-backend.service --no-pager
```

Keep old releases until the new release has been verified.

## What to send back after the first deploy

After the first manual deploy, send back:

- The deployed version.
- Confirmation that `/opt/languagevoicetutor/backend/current` points to the intended release.
- Redacted `systemctl status languagevoicetutor-backend.service --no-pager` output.
- Redacted `journalctl -u languagevoicetutor-backend.service -n 100 --no-pager` output.
- Results for `curl -fsS http://127.0.0.1:5001/health`.
- Results for `curl -fsS http://127.0.0.1:5001/api/health/database`.
- Results for `curl -fsS https://api.languagevoicetutor.com/health` after nginx/TLS are configured.
- Confirmation that no secrets were printed or committed.
- Any server warnings, failed commands, or changed assumptions.

## Password reset SMTP environment (2026-06-08)

Password reset email delivery is configured only through server environment variables, normally in `/etc/languagevoicetutor/backend.env`. Do not commit real SMTP credentials.

Example placeholders:

```bash
PasswordReset__Enabled=true
PasswordReset__TokenLifetimeMinutes=60
PasswordReset__ResetUrlBase=
PasswordReset__RequireConfiguredEmailSender=true
SmtpEmail__Enabled=true
SmtpEmail__Host=smtp.example.com
SmtpEmail__Port=587
SmtpEmail__UseStartTls=true
SmtpEmail__UserName=change-me
SmtpEmail__Password=change-me
SmtpEmail__FromAddress=support@languagevoicetutor.com
SmtpEmail__FromName=Language Voice Tutor Support
```

If password reset is enabled and `PasswordReset__RequireConfiguredEmailSender=true`, the backend requires a configured SMTP sender before accepting reset requests. External tester handoff remains blocked until CMS server verification, a basic public download page, a basic update UI/system, clean-machine smoke, and checklist completion are done.
