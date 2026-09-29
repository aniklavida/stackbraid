# Contributing

StackBraid is pre-release. The `Identity` feature is implemented and tested
across every shipped backend, and the web and admin shells are implemented and
verified against both backends. The remaining work is the professional layer,
the last database leg and release hardening. The repository's
[`docs/ROADMAP.md`](docs/ROADMAP.md) records what is done and what is not.

**Pull requests are welcome.** Small, well-scoped fixes and documentation
corrections are especially welcome.

## Before you start

1. Read [`AGENTS.md`](AGENTS.md) first — it is the contract for humans and
   agents alike.
2. The API contract comes first. Never change an API by editing a backend.
3. A change that cannot be made end-to-end in every shipped stack will not be
   merged.
4. Every new dependency needs a licence audit in the pull request. See the
   dependency rules in `AGENTS.md`.
5. Run the conformance suite against every backend before opening a pull
   request.
6. Generated clients are never hand-edited. Regenerate them.

## Running the tests

Each piece ships its own `README.md` with the exact commands for that stack.
Start there — this file is copied into every project the picker produces, so
it stays deliberately stack-neutral.

Two things are true everywhere:

- **Integration and conformance suites need a real database, and they do not
  create one.** They do not use Docker or Testcontainers. Every backend ships a
  `scripts/start-local-postgres.sh` that `initdb`s a throwaway cluster into a
  temp directory, starts it on a free port and prints the connection string,
  and a matching `stop-local-postgres.sh` that stops it and deletes its data.
  Nothing is left behind. Without the connection string the integration tests
  fail fast with a message naming the variable, rather than silently skipping —
  so a green run always means the database was really there.
- **Unit, architecture, lint and licence suites need no database at all.**

If a test fails and the fix is not obvious,
[`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md) documents the failures
people actually hit, with their real causes and fixes.

## Generated clients

The clients are generated from `contract/openapi.yaml` — see their own
`README.md` files. Regenerate with `./scripts/generate-clients.sh`; never
hand-edit the output.

Enable the drift-checking pre-commit hook once per clone:

```bash
git config core.hooksPath .githooks
```

It regenerates both clients and fails the commit if `clients/` doesn't match
a fresh generation — see `scripts/check-client-drift.sh`.

The same check also runs in CI (`.github/workflows/ci.yml`, job
`client-drift`) on every push and pull request, so drift is caught even from
a clone where the hook was never enabled, or a commit made with
`--no-verify`.

## Before you open a pull request

```bash
node scripts/check-dependency-licenses.mjs   # if you touched any dependency
node scripts/check-translation-keys.mjs      # if you touched any user-facing string
./scripts/check-client-drift.sh              # if you touched contract/openapi.yaml
```

CI runs the same checks plus shellcheck, yamllint, an OpenAPI lint and the
full backend, frontend, mobile and conformance matrix.

## Commit messages

Describe what changed and why. Reference the contract change if there was one.

## Reporting a security issue

Do not open a public issue. See [`SECURITY.md`](SECURITY.md).
