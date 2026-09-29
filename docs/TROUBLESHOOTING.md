# Troubleshooting

The failures people actually hit, and what each one means. Every command here
runs in the foreground and needs no container engine.

## Backend integration tests fail with a message naming a variable

**Symptom.** The .NET suite fails immediately with
`STACKBRAID_TEST_POSTGRES_CONNECTION_STRING` in the message; the Python suite
reports a skip naming `STACKBRAID_POSTGRES_DSN`.

**Cause.** Integration tests need a real database, and they do not create one.
Neither backend uses Docker or Testcontainers, by choice — see
[`SPEC.md` §15](SPEC.md#15--tests).

**Fix.**

```bash
cd backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
dotnet test StackBraid.sln
./scripts/stop-local-postgres.sh
```

The script `initdb`s a cluster into a temp directory on a free port and prints
the connection string on stdout. `stop-local-postgres.sh` stops it and deletes
its data directory. The tests **fail fast** on a missing variable rather than
skipping, so a green run always means the database was really there.

## `.NET + MySQL` does not build

**Symptom.** `Pomelo.EntityFrameworkCore.MySql` cannot build its model on
EF Core 10.

**Cause.** Pomelo 9.0.0 — the only MySQL EF Core provider with an accepted
licence, `MySql.Data` being GPL-2.0 — targets EF Core 9. This is a genuine
block, not a misconfiguration. Its migration is generated and committed, and
the CI leg is `continue-on-error` until Pomelo ships an EF Core 10 release.

**Workaround.** Use PostgreSQL or SQL Server on .NET. Python's MySQL leg is
wired and passing.

## A conformance run reports skips

**Symptom.** Checks named as `Skipped`, typically the privileged user and role
checks.

**Cause.** The suite runs as a freshly registered user, which holds no
administrator permission. Conformance cannot prove admin behaviour without an
admin.

**Fix.** Seed an administrator and point the suite at it.

```bash
export CONFORMANCE_ADMIN_EMAIL=admin@stackbraid.local
export CONFORMANCE_ADMIN_PASSWORD='ChangeMe!123'
```

## The access-token expiry check skips

**Symptom.** `GET /v1/auth/refresh — a refreshed access token works` skips
rather than runs.

**Cause.** The backend's access-token lifetime is longer than the suite's
wait, so the token has not expired yet and the check is not meaningful.

**Fix.** Shorten the lifetime. `.NET`: `Jwt__AccessTokenLifetimeSeconds=5`.
Python: `STACKBRAID_JWT_ACCESS_TOKEN_LIFETIME_SECONDS=5`. Both are the
values CI uses.

## The job and realtime checks report 404

**Symptom.** `POST /v1/jobs/scenarios` returns 404.

**Cause.** That endpoint is opt-in — it exists to prove the durable-job
plumbing and is off unless explicitly enabled.

**Fix.** `.NET`: `Jobs__ConformanceEnabled=true`. Python:
`STACKBRAID_JOBS_CONFORMANCE_ENABLED=true`.

## The generated clients no longer match the contract

**Symptom.** The `client-drift` CI job fails, or the pre-commit hook rejects
the commit.

**Cause.** `clients/` is generated and committed. Editing it by hand, or
editing `contract/openapi.yaml` without regenerating, is drift.

**Fix.** Regenerate, never hand-edit.

```bash
./scripts/generate-clients.sh
git config core.hooksPath .githooks   # once per clone, enables the pre-commit drift check
```

`CONTRIBUTING.md` covers this in full.

## A CI job fails on a dependency licence

**Symptom.** `VERSION DRIFT: <package> resolved at X, but the inventory only
audited Y`.

**Cause.** Working as designed. Every dependency here is inherited by every
user of this skeleton, so a version bump must be re-audited, not waved
through.

**Fix.** Re-verify the package's licence at the new version against its
registry metadata or licence file, add or update its entry in
`docs/dependency-inventory.json`, and record the date in
[`DEPENDENCIES.md`](DEPENDENCIES.md). Do not carry the old licence forward.
See the `review-dependency` playbook.

## The conformance self-check job is red

**Symptom.** `npm run demo:violations` reports a non-zero exit on the
`baseline (no injected violations)` scenario, while every violation scenario
reports `OK`.

**Cause.** The baseline is the one scenario that must exit 0. A non-zero
baseline with passing violation scenarios almost always means the suite and
the stub server have drifted apart — a check was added that the stub does not
satisfy, or the stub returns a shape the check rejects.

**How to read the output.** Each scenario states its expected and actual exit
code. Every scenario except `baseline` is *expected* to fail; that is the
suite detecting an injected violation. Only `baseline` must pass.

## A translation-key check fails

**Symptom.** `node scripts/check-translation-keys.mjs` reports a key present in
one locale and missing in another.

**Cause.** A user-facing string was added or renamed in one catalogue only.

**Fix.** Add the key to every locale, not just the default. The supported set
is `en` and `es` across the backends, both frontends and mobile.

## A frontend cannot reach the backend from a browser

**Symptom.** Every request from the browser fails on CORS.

**Cause.** Both backends ship an empty, inert CORS allow-list. A
browser-based frontend is a different origin, so it needs an explicit entry.

**Fix.** `.NET`: `Cors:AllowedOrigins`. Python:
`STACKBRAID_CORS_ALLOWED_ORIGINS_RAW`. Both accept a comma-separated list.

## A frontend's session does not survive a reload

**Symptom.** The app drops to the sign-in screen on refresh.

**Cause.** The refresh cookie is scoped to the backend's own `/v1/auth` path
and is httpOnly, so the frontend's own server can never see it. Next.js
therefore cannot hydrate a session in middleware or a proxy file; it hydrates
on the client instead, from a call the browser can make to the backend
directly. Angular has no equivalent constraint — its functional route guards
await the cookie hydration before deciding.

**Fix.** This is the shipped design, not a bug. See
`frontends/nextjs/README.md` for the reasoning.

## `flutter` cannot run on a desktop target

**Symptom.** Flutter integration tests fail to build a macOS target.

**Cause.** The mobile shell is verified on the **macOS desktop target only**.
It has not been verified on an iOS or Android simulator, because none was
available when it was built.

**Fix.** Run against macOS, or install a simulator target. `flutter test`
(unit tests) runs on any host and needs no device.

## Docker or a container engine is unavailable

**Symptom.** Anything that wants a database or the infrastructure stack to
start, with Docker daemon errors or a credential-helper failure.

**Cause.** Docker is **not required** for anything in the two checklists this
pass covers. Every backend test, every conformance run, and every
client-drift/licence/translation check runs with no container engine at all.

**Fix.** Use `start-local-postgres.sh` as above. The one thing Docker is
genuinely needed for is `docker compose up` against `infra/compose.yaml`,
which has not been verified end to end on a clean machine — see
[`ROADMAP.md`](ROADMAP.md).

## An architecture test fails

**Symptom.** NetArchTest (`.NET`) or import-linter (Python) reports a layering
violation.

**Cause.** Either a real boundary break, or a legitimate new dependency that
the rule has not been taught about.

**Fix.** A real break is a real bug — move the code. A rule that is genuinely
too narrow belongs in `LayeringRulesTests.cs` or the `[tool.importlinter]`
contracts in `pyproject.toml`, changed deliberately rather than to silence a
failure. The `review-architecture` playbook covers both cases.
