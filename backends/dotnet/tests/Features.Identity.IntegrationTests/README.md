# Features.Identity.IntegrationTests

Runs migrations and repository behaviour against a real Postgres — no
Testcontainers, no Docker. The connection string comes from an environment
variable, so a contributor's machine, this repository's CI, and any other
empty Postgres database all work the same way.

## Running locally

```bash
cd backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
dotnet test tests/Features.Identity.IntegrationTests
./scripts/stop-local-postgres.sh
```

`start-local-postgres.sh` `initdb`s a throwaway cluster into a temp
directory, starts it on a free port, creates one empty database, and prints
an ADO-style connection string on stdout. `stop-local-postgres.sh` stops
that cluster and deletes its data directory — nothing is left behind.

If `STACKBRAID_TEST_POSTGRES_CONNECTION_STRING` is not set, these tests
fail fast with a message naming the variable and the script to run, rather
than silently skipping or reporting a false pass.

## Why not Testcontainers

Testcontainers is `docs/SPEC.md`'s named choice and remains this project's
target — nothing here rejects it. It needs a running container engine,
which was unavailable while this suite was first written; reading the
connection string from an environment variable keeps the tests themselves
unchanged either way; a CI workflow (or a future local setup with a
container engine) can supply one from Testcontainers, a service container,
or this project's own scripts interchangeably.
