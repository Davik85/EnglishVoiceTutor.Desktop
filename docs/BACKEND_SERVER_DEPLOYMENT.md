# Backend Server Deployment Foundation

This document describes the prepared deployment foundation for the Language Voice Tutor backend on Ubuntu 24.04. It is a manual backend-only deployment workflow. It does not upload Windows release files, run EF migrations, publish CMS runtime content, enable production billing, or make the product broadly public production-ready.

## Current server verification for 0.1.35-backend.3

The production backend active release is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.3`, and `/opt/languagevoicetutor/backend/current` points to that release. Backend `0.1.35-backend.3` contains the lesson chat invalid OpenAI response resilience fix. No EF migration was run or needed for this backend-only fix. Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.2`. The earlier refresh-token migration `20260611000000_AddUserRefreshTokens` remains applied.

The production backend is reachable at `https://api.languagevoicetutor.com`. `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`. The service `languagevoicetutor-backend.service` started successfully after deploy. Operator manual smoke should continue to verify app launch, login, Account opening, lesson start, at least 7 Daily Life / Introductions or guided roleplay user messages without a generic server error, Lesson History updates, and Progress updates.

Do not copy server secrets, passwords, API keys, private keys, tokens, private environment values, private IP-sensitive credentials, or provider credentials into this document or any tracked file.

## Current production backend snapshot

Last known production backend snapshot: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.3` is active via `/opt/languagevoicetutor/backend/current`. Verify the live value with `ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"` before calling any backend version current. Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.2`.

Backend `0.1.35-backend.3` did not require an EF migration. The earlier refresh-token migration `20260611000000_AddUserRefreshTokens` remains applied. Production backend health and database health are healthy at the documented HTTPS health endpoints. Backend deploys remain separate from EF database migrations; do not run migrations automatically from the upload/deploy flow. Backend deploys also remain separate from Windows release upload; no Windows installer upload was performed for `0.1.35-backend.3`.

## Scope

- Static Windows installer hosting and backend API hosting are separate deployment tracks.
- The current static HTTPS site is already available at `languagevoicetutor.com` for public/static hosting work.
- The backend API is available at `api.languagevoicetutor.com`.
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
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.35-backend.3
```

This publishes `backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj` in Release mode for `linux-x64` as a self-contained deployment and writes:

```text
artifacts\publish\backend-linux-x64
artifacts\packages\backend\LanguageVoiceTutor.Backend-linux-x64-0.1.35-backend.3.zip
```

The production server does not need a git checkout, `dotnet` SDK, or `dotnet` runtime for this self-contained package. Generated files under `artifacts/` must not be committed. Do not rebuild or replace desktop release artifacts as part of backend deployment.

## Upload dry-run

Dry-run can build the archive locally, then print the SSH/SCP/systemd commands without uploading or restarting anything:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version 0.1.35-backend.3 `
  -PackageFirst `
  -DryRun
```

The script defaults to SSH host `lvt-server`, user `deploy`, and remote path `/opt/languagevoicetutor/backend`. Override `-ServerHost`, `-ServerUser`, `-RemotePath`, or `-SshPort` only from the local operator environment. Do not commit IP addresses, SSH key paths, passwords, tokens, or secrets.

## Upload and restart

For a reviewed backend-only deployment, run from Windows. Keep any EF migration as a separate explicit operation; `0.1.35-backend.3` did not need one:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version 0.1.35-backend.3 `
  -PackageFirst
```

The upload script creates an ignored local deployment helper under `artifacts/temp/backend-linux-upload/<version>/deploy-backend-release.sh`, uploads the backend archive and helper script to `/opt/languagevoicetutor/backend/uploads/<version>/`, then runs the uploaded helper with `bash /opt/languagevoicetutor/backend/uploads/<version>/deploy-backend-release.sh`. The helper creates `/opt/languagevoicetutor/backend/releases/<version>`, unzips the archive, validates `EnglishVoiceTutor.Api`, sets its executable bit, atomically switches `/opt/languagevoicetutor/backend/current` through `current.next`, and records `/opt/languagevoicetutor/backend/previous` when an older current release exists. Keeping the release extraction and symlink switch in a normal `.sh` file avoids fragile nested PowerShell/SSH/bash inline quoting. The script then restarts `languagevoicetutor-backend.service` and prints service status as separate SSH commands. Because these `sudo systemctl` commands may require an interactive sudo password prompt, the restart and status SSH commands allocate a pseudo-terminal with `ssh -tt`; non-sudo upload and helper commands keep normal non-TTY SSH. Pass `-NoRestart` only when a separate controlled restart is planned; with `-NoRestart`, the script does not run the sudo restart or sudo status commands. In `-DryRun` mode, these TTY sudo commands are printed but not executed. The script never runs EF migrations and never reads or prints production database secrets.

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

# Windows/operator equivalent:
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

## EF migrations

The refresh-token backend requires migration `20260611000000_AddUserRefreshTokens`. Apply it to production before or alongside deploying backend code that issues or validates refresh tokens. Do not run migrations automatically from the upload script. Apply EF migrations deliberately and explicitly only after reviewing the target database, backups, environment, and migration list.

Generate a reviewed SQL script locally from the last known production migration to the refresh-token migration:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-backend-refresh-token-migration-sql.ps1
```

This writes a generated SQL file under `artifacts\sql\backend`, which must not be committed. If the exact previous production migration is uncertain, generate an idempotent script instead and review it carefully:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-backend-refresh-token-migration-sql.ps1 -Idempotent
```

Preferred production apply options are:

1. From a trusted admin machine with direct PostgreSQL access and `psql` installed, apply the reviewed SQL while letting `psql` prompt for the password so the password is not written into shell history or logs:

   ```powershell
   psql "host=<db-host> port=5432 dbname=lvt_app_db user=lvt_app sslmode=require" -v ON_ERROR_STOP=1 -f .\artifacts\sql\backend\20260611000000_AddUserRefreshTokens.from-20260604121000.sql
   ```

2. If the server has `psql` and peer/admin database access, upload only the reviewed SQL file and apply it without printing the application connection string:

   ```powershell
   scp .\artifacts\sql\backend\20260611000000_AddUserRefreshTokens.from-20260604121000.sql deploy@lvt-server:/tmp/20260611000000_AddUserRefreshTokens.sql
   ssh deploy@lvt-server "sudo -u postgres psql -d lvt_app_db -v ON_ERROR_STOP=1 -f /tmp/20260611000000_AddUserRefreshTokens.sql && rm -f /tmp/20260611000000_AddUserRefreshTokens.sql"
   ```

Do not echo `ConnectionStrings__DefaultConnection`, `PGPASSWORD`, or database URLs into terminal logs. Do not commit generated SQL or any file containing database credentials.

## Rollback idea

Each upload extracts to a versioned folder under:

```text
/opt/languagevoicetutor/backend/releases/{version}
```

To roll back manually, point `current` to a previous release and restart the service:

```bash
sudo ln -sfn /opt/languagevoicetutor/backend/releases/<previous-version> /opt/languagevoicetutor/backend/current.next
sudo mv -Tf /opt/languagevoicetutor/backend/current.next /opt/languagevoicetutor/backend/current
sudo systemctl restart languagevoicetutor-backend.service
sudo systemctl status languagevoicetutor-backend.service --no-pager
```

If `/opt/languagevoicetutor/backend/previous` exists and points to the desired release, rollback can use that symlink target. Keep old releases until the new release has been verified. Database migrations are normally not rolled back automatically; confirm compatibility before switching code backward after a schema change.

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

## Backend upload executable verification update (2026-06-08)

The backend Linux package/upload flow now creates ZIP entries with Linux-friendly forward slashes and runs deployment through a single remote `bash -lc` invocation. After extraction it explicitly checks:

```bash
test -f /opt/languagevoicetutor/backend/releases/<version>/EnglishVoiceTutor.Api
test -x /opt/languagevoicetutor/backend/releases/<version>/EnglishVoiceTutor.Api
```

The upload script applies `chmod 755` to the main backend executable and does not swallow chmod/test failures. If the executable is missing or not executable, deployment fails loudly and must not be treated as successful. The script still does not write secrets and does not run EF migrations.

Password reset SMTP values, including credentials, must remain server-only in `/etc/languagevoicetutor/backend.env`. The production Zoho-style settings may use `SmtpEmail__Username` and `SmtpEmail__UseSsl`; no SMTP password or raw reset token should be logged or committed.
