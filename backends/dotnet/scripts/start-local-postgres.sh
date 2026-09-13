#!/usr/bin/env bash
# Starts a throwaway local Postgres cluster for integration tests — no
# Docker, no Testcontainers. initdb's a fresh cluster into a temp
# directory, starts it on a free port, creates one database, and prints an
# ADO-style connection string on stdout (and nothing else), so it can be
# captured directly:
#
#   export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
#   dotnet test tests/Features.Identity.IntegrationTests
#   ./scripts/stop-local-postgres.sh
#
# State (PGDATA path, port, PID) is recorded in
# backends/dotnet/.local-postgres-test-state so stop-local-postgres.sh can
# find what to tear down without guessing.
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
state_file="$root_dir/.local-postgres-test-state"

if [ -f "$state_file" ]; then
  echo "A local test Postgres is already recorded at $state_file — run stop-local-postgres.sh first." >&2
  exit 1
fi

pg_data_dir="$(mktemp -d "${TMPDIR:-/tmp}/stackbraid-test-postgres.XXXXXX")"

# Ask the OS for a free TCP port rather than guessing one that might collide.
port="$(python3 -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1]); s.close()')"

echo "Initializing a throwaway Postgres cluster in $pg_data_dir on port $port..." >&2
initdb --username=postgres --auth=trust --no-sync --pgdata="$pg_data_dir" >&2

pg_ctl -D "$pg_data_dir" -o "-p $port -k $pg_data_dir -h 127.0.0.1" -l "$pg_data_dir/postgres.log" start >&2

# Wait for the cluster to accept connections before handing back a connection string.
for _ in $(seq 1 30); do
  if pg_isready -h 127.0.0.1 -p "$port" -U postgres > /dev/null 2>&1; then
    break
  fi
  sleep 0.5
done

createdb -h 127.0.0.1 -p "$port" -U postgres stackbraid_test >&2

cat > "$state_file" <<EOF
PG_DATA_DIR=$pg_data_dir
PG_PORT=$port
EOF

echo "Host=127.0.0.1;Port=$port;Database=stackbraid_test;Username=postgres;Password=postgres"
