#!/usr/bin/env bash
set -euo pipefail

DATABASE_NAME="${DATABASE_NAME:-lvt_app_db}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/languagevoicetutor/postgres}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
DRY_RUN=0
ORIGINAL_ARGS=("$@")

usage() {
  cat <<'USAGE'
Usage: backup_lvt_postgres.sh [--dry-run]

Creates a local PostgreSQL custom-format backup as the postgres service account,
verifies readability with pg_restore --list, and removes old local backups that
match the configured safe filename pattern.

Environment overrides:
  DATABASE_NAME     Database name to back up. Default: lvt_app_db
  BACKUP_DIR        Backup directory. Default: /var/backups/languagevoicetutor/postgres
  RETENTION_DAYS    Local retention window in days. Default: 14

Safety notes:
  - Does not read /etc/languagevoicetutor/backend.env.
  - Does not print connection strings, passwords, dump contents, table data, or raw user data.
  - Deletes only BACKUP_DIR/DATABASE_NAME_*.dump files after validating BACKUP_DIR.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "ERROR: unsupported argument: $1" >&2
      usage >&2
      exit 64
      ;;
  esac
done

if [[ ! "$DATABASE_NAME" =~ ^[A-Za-z0-9_]+$ ]]; then
  echo "ERROR: DATABASE_NAME must contain only letters, numbers, and underscores." >&2
  exit 64
fi

if [[ ! "$RETENTION_DAYS" =~ ^[0-9]+$ ]] || [[ "$RETENTION_DAYS" -lt 1 ]]; then
  echo "ERROR: RETENTION_DAYS must be a positive integer." >&2
  exit 64
fi

if [[ "$(id -un)" != "postgres" ]]; then
  if command -v sudo >/dev/null 2>&1; then
    exec sudo -u postgres env DATABASE_NAME="$DATABASE_NAME" BACKUP_DIR="$BACKUP_DIR" RETENTION_DAYS="$RETENTION_DAYS" "$0" "${ORIGINAL_ARGS[@]}"
  fi

  echo "ERROR: this script must run as the postgres service account, or sudo must be available to re-exec as postgres." >&2
  exit 77
fi

case "$BACKUP_DIR" in
  ""|"/"|"/var"|"/var/"|"/var/backups"|"/var/backups/"|"/tmp"|"/tmp/")
    echo "ERROR: refusing dangerous BACKUP_DIR value: '${BACKUP_DIR}'" >&2
    exit 64
    ;;
esac

if [[ "$BACKUP_DIR" != /* ]]; then
  echo "ERROR: BACKUP_DIR must be an absolute path." >&2
  exit 64
fi

if [[ "$BACKUP_DIR" == *".."* ]]; then
  echo "ERROR: BACKUP_DIR must not contain '..'." >&2
  exit 64
fi

if [[ "$BACKUP_DIR" != "/var/backups/languagevoicetutor/postgres" && "$BACKUP_DIR" != /var/backups/languagevoicetutor/postgres/* ]]; then
  echo "ERROR: BACKUP_DIR must be /var/backups/languagevoicetutor/postgres or a child directory." >&2
  exit 64
fi

command -v pg_dump >/dev/null 2>&1 || { echo "ERROR: pg_dump was not found in PATH." >&2; exit 69; }
command -v pg_restore >/dev/null 2>&1 || { echo "ERROR: pg_restore was not found in PATH." >&2; exit 69; }
command -v find >/dev/null 2>&1 || { echo "ERROR: find was not found in PATH." >&2; exit 69; }

install -d -m 0750 "$BACKUP_DIR"

timestamp="$(date -u +%Y%m%d_%H%M%SZ)"
backup_file="${BACKUP_DIR}/${DATABASE_NAME}_${timestamp}.dump"
restore_list_file="$(mktemp /tmp/lvt_pg_restore_list.XXXXXX)"
cleanup_restore_list() {
  rm -f "$restore_list_file"
}
trap cleanup_restore_list EXIT

pg_dump --format=custom --no-owner --no-acl --file="$backup_file" "$DATABASE_NAME"

test -s "$backup_file"
pg_restore --list "$backup_file" >"$restore_list_file"
restore_list_line_count="$(wc -l <"$restore_list_file" | tr -d '[:space:]')"
backup_size_bytes="$(stat -c '%s' "$backup_file")"

removed_count=0
would_remove_count=0
retention_summary="no old local backups matched retention policy"

if [[ "$DRY_RUN" -eq 1 ]]; then
  while IFS= read -r -d '' old_backup; do
    printf 'DRY-RUN: would remove old local backup: %s\n' "$old_backup"
    would_remove_count=$((would_remove_count + 1))
  done < <(find "$BACKUP_DIR" -maxdepth 1 -type f -name "${DATABASE_NAME}_*.dump" -mtime +"$RETENTION_DAYS" -print0)
  retention_summary="dry-run; would remove ${would_remove_count} old local backup file(s) older than ${RETENTION_DAYS} day(s)"
else
  while IFS= read -r -d '' old_backup; do
    rm -f -- "$old_backup"
    removed_count=$((removed_count + 1))
  done < <(find "$BACKUP_DIR" -maxdepth 1 -type f -name "${DATABASE_NAME}_*.dump" -mtime +"$RETENTION_DAYS" -print0)
  retention_summary="removed ${removed_count} old local backup file(s) older than ${RETENTION_DAYS} day(s)"
fi

echo "PostgreSQL local backup completed safely."
echo "Backup file: ${backup_file}"
echo "Backup size bytes: ${backup_size_bytes}"
echo "pg_restore list line count: ${restore_list_line_count}"
echo "Retention action: ${retention_summary}"
