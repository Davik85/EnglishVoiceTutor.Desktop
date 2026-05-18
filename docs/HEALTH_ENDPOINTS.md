# Backend Health Endpoints

These lightweight endpoints are intended for local and development verification before the desktop app is connected to backend-backed user settings. They do not require authentication and they do not expose secrets, connection strings, database passwords, database hosts, or database usernames.

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

The database health response includes only the EF Core provider name and a short safe error message. It must not include connection strings, passwords, hosts, usernames, or other secrets.
