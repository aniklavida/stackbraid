# Conformance suite

One test suite, written against `contract/openapi.yaml`, runnable against any
backend on any database provider. It takes a base URL and nothing else — it
has no knowledge of which backend is running.

**Status:** the runner exists. The checks themselves (auth, users, roles, the
error envelope) land in a follow-up commit. No backend exists yet to run this
against (see `docs/SPEC.md`); a deliberately non-conforming stub server, used
to prove the suite actually catches violations, is coming with the checks.

## Usage

```bash
node cli/run.mjs <baseUrl>
# e.g.
node cli/run.mjs http://localhost:8080
```

Exits `0` if every check passed or was explicitly skipped, non-zero if any
check failed — so CI can gate on it.

## Design

- **Zero runtime dependencies.** Node's built-in `fetch`, `assert`-style
  failure objects, and a hand-rolled schema validator (arriving with the
  checks) are enough for this job, and zero dependencies means zero licences
  to audit for a tool every user of this skeleton runs in CI.
- **Every failure names the field and the expected value.** "Assertion
  failed" is useless at 2am; see `src/assert.mjs`.
- **No knowledge of the backend.** The suite talks to the contract, not to
  .NET or Python, Postgres or MySQL. Anything backend-specific (e.g. an
  admin login used only for permission-gated endpoints) is opt-in through an
  environment variable, never required.
