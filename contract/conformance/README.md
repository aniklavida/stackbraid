# Conformance suite

One test suite, written against `contract/openapi.yaml`, runnable against any
backend on any database provider. It takes a base URL and nothing else — it
has no knowledge of which backend is running.

**Status:** the runner, every check, and a stub fixture that proves the
checks actually fail (and fail for the right reasons) all exist. No backend
exists yet to run this against for real (see `docs/SPEC.md`) — a clean run
against `fixtures/stub-server/` is evidence the suite works, never evidence
any real backend conforms to anything.

## Usage

```bash
node cli/run.mjs <baseUrl>
# e.g.
node cli/run.mjs http://localhost:8080
```

Exits `0` if every check passed or was explicitly skipped, non-zero if any
check failed — so CI can gate on it.

### Optional environment variables

| Variable | Purpose |
|---|---|
| `CONFORMANCE_ADMIN_EMAIL` / `CONFORMANCE_ADMIN_PASSWORD` | Credentials for a seeded administrator, used only for permission-gated endpoints (listing/managing users and roles). Without these, the suite falls back to the user it registers for itself; a resulting `403` on those specific checks is reported as **skipped**, not failed, with the reason named. |
| `CONFORMANCE_MAX_EXPIRY_WAIT_MS` | How long the suite is willing to wait for a live access token to actually expire (default `65000`). The suite reads the token's own `expiresAt` and only exercises the "expired token" check if the wait fits this budget — see "What this does not (and cannot) do" below. |

## What it checks

- **Shape and type of every response field** — not just status codes. Every
  `User`, `Role`, `TokenPair` and `Page<T>` in every response is validated
  field by field against `contract/openapi.yaml`'s schemas.
- **The error envelope** — every non-2xx response must be RFC 9457 Problem
  Details (`application/problem+json`) with `type`/`title`/`status` plus
  StackBraid's `code`/`traceId` extension, `errors` on validation failures,
  no undocumented extra fields, and a `traceId` that actually correlates one
  request.
- **Pagination** — `GET /v1/users` returns `page`/`pageSize`/`totalItems`/
  `totalPages` (offset, per card 1's decision), `totalPages` is arithmetically
  consistent, and a cursor-shaped field (`nextCursor` and friends) leaking in
  is treated as a violation.
- **Timestamps** — every `UtcDateTime` field is checked against the exact
  pattern pinned in the contract: RFC 3339, UTC, a trailing `Z`, never a
  numeric offset like `+00:00`.
- **Auth: refreshed / expired / revoked** — a token from `/v1/auth/refresh`
  works; a token rotated out by a later refresh, or explicitly revoked by
  `/v1/auth/logout`, is rejected; an access token is rejected once its own
  `expiresAt` has passed.
- **Both token-delivery paths from card 1** — `login`, `refresh` and
  `logout` are each exercised with tokens passed in the request body
  (mobile/API clients) *and* via the httpOnly `refreshToken` cookie with no
  body (browser clients), including that the cookie carries
  `HttpOnly; Secure; SameSite=Strict` and the same token value as the body.

## What this does not (and cannot) do

The suite is given only a base URL — it cannot force a real backend's access
token to expire on demand. It reads the `expiresAt` the backend itself
returned and either waits for genuine expiry (if that fits within
`CONFORMANCE_MAX_EXPIRY_WAIT_MS`) or skips that one check with a clear
reason. Run the backend under test with a short-lived access token TTL to
exercise it for real; the stub fixture (see `fixtures/stub-server/`) does
exactly that by default so the check always runs there.

## Proving it works

`fixtures/stub-server/` is a deliberately non-conforming stub of the
Identity API, used only as a test fixture — see its own README for what it
is and, just as importantly, what it is not. `scripts/demo-violations.mjs`
runs the real suite against that stub in its baseline (conforming) mode and
then again with each of the five violation classes card 2 named — wrong
type, missing field, wrong timestamp format, wrong error shape, wrong
pagination — injected one at a time, and checks the suite's exit code
matches what should happen in each case:

```bash
npm run demo:violations
# or: node scripts/demo-violations.mjs
```

This is the actual CI gate for the suite itself (see
`.github/workflows/conformance.yml`, added next): if a future change to the
suite ever stopped catching one of these classes, this is what would turn
CI red.

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
