#!/usr/bin/env bash
# Stops and deletes the throwaway cluster start-local-postgres.sh created.
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
state_file="$root_dir/.local-postgres-test-state"

if [ ! -f "$state_file" ]; then
  echo "No local test Postgres state file found at $state_file — nothing to stop." >&2
  exit 0
fi

# shellcheck disable=SC1090
source "$state_file"

if [ -n "${PG_DATA_DIR:-}" ] && [ -d "$PG_DATA_DIR" ]; then
  echo "Stopping the throwaway Postgres cluster in $PG_DATA_DIR..." >&2
  pg_ctl -D "$PG_DATA_DIR" stop -m fast >&2 || true
  rm -rf "$PG_DATA_DIR"
fi

rm -f "$state_file"
echo "Stopped and removed." >&2
