# Backend Health Endpoints

These lightweight endpoints support local/development checks and the authenticated production Admin CMS health strip. The endpoints themselves do not require authentication. Their responses intentionally expose only safe status information, never secrets, connection strings, database passwords, database hosts, or database usernames. The Admin CMS header displays only the status derived from each response.

## `GET /api/health`

Checks that the backend process can respond without touching the database.

### Healthy backend response

Expected status code: `200 OK`

```json
{
  "status": "Healthy",
  "environment": "Development",
  "checkedAtUtc": "2026-05-18T12:00:00+00:00"
}
```

## `GET /api/health/database`

Checks whether the backend can connect to the configured database through the existing EF Core `AppDbContext`.

### Healthy database response

Expected status code: `200 OK`

```json
{
  "status": "Healthy",
  "canConnect": true,
  "provider": "Npgsql.EntityFrameworkCore.PostgreSQL",
  "checkedAtUtc": "2026-05-18T12:00:00+00:00",
  "error": null
}
```

### Unavailable database response

Expected status code: `503 Service Unavailable`

```json
{
  "status": "Unhealthy",
  "canConnect": false,
  "provider": "Npgsql.EntityFrameworkCore.PostgreSQL",
  "checkedAtUtc": "2026-05-18T12:00:00+00:00",
  "error": "Database connection is unavailable."
}
```

The database health response includes only the EF Core provider name and a short safe error message. It must not include connection strings, passwords, hosts, usernames, or other secrets. The Admin CMS health strip does not display the provider or error field.

The strip also reads `GET /api/backend/config-status` for **AI Config**. This reports whether OpenAI is configured; it is not a live OpenAI provider-health check and does not test provider access. **CMS Runtime** uses the authenticated `GET /api/admin/dev/cms/runtime-status` diagnostic, subject to `cms.runtime_status.read`; admins without that permission see a neutral unavailable state. A working static JSON fallback appears as a warning.

The strip is informational. Investigate a warning or error with the appropriate deeper diagnostics before drawing conclusions about the full production system.
