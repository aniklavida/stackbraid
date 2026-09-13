# Conformance stub — a test fixture, not a backend

`server.mjs` is a deliberately minimal, in-memory implementation of
`contract/openapi.yaml`'s `Identity` surface. It exists for exactly one
purpose: proving `contract/conformance` actually catches contract
violations, and does so for five specific classes — wrong type, missing
field, wrong timestamp format, wrong error shape, wrong pagination.

**This is not, and must never be read as, a real backend.** It has no
persistence, no permission enforcement, no localization, and skips whole
classes of behaviour (rate limiting, most validation) the real .NET and
Python backends will implement. A clean run of the conformance suite
against this stub is evidence the *suite* works. It is not evidence any
real backend conforms to anything — no backend exists yet (see
`docs/SPEC.md`).

## Running it

```bash
node server.mjs
# StackBraid conformance stub listening on http://localhost:4100 (access token TTL 5000ms)
```

Environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `PORT` | `4100` | Port to listen on. |
| `STUB_ACCESS_TTL_MS` | `5000` | Access token lifetime. Short by design, so the conformance suite's expiry check actually waits and observes a real 401 instead of skipping. |
| `STUB_VIOLATIONS` | *(unset)* | Comma-separated list of violations to inject (see below). Unset means the baseline: a best-effort faithful implementation of the contract. |

## Violation classes

Each corrupts a distinct part of the response, matching the five classes the
conformance suite checks for:

| Value | What it does |
|---|---|
| `wrong-type` | `User.status` becomes a boolean instead of the `active`/`inactive` enum string; `Page.totalItems` becomes a string; `Role.permissions` becomes a comma-joined string instead of an array; `TokenPair.expiresAt` becomes a unix-seconds number instead of an RFC 3339 string. |
| `missing-field` | Drops `User.displayName`, `TokenPair.tokenType`, `Page.totalPages` and `Role.description` from their respective responses. Never touches `accessToken`/`refreshToken` themselves — that would break the body-vs-cookie tests, which are a different concern entirely. |
| `bad-timestamp` | Every `UtcDateTime` field is emitted with a numeric `+00:00` offset instead of a trailing `Z` — the exact divergence a spike caught between .NET and Python, recorded in `contract/openapi.yaml`'s `UtcDateTime` description. |
| `bad-error-shape` | Every error response becomes `{ "error": "...", "message": "..." }` with `content-type: application/json`, instead of RFC 9457 `application/problem+json` with `type`/`title`/`status`/`code`/`traceId`. |
| `bad-pagination` | `GET /v1/users` returns `{ items, nextCursor }` instead of the offset envelope `{ page, pageSize, totalItems, totalPages, items }`. |

Multiple values may be combined (`STUB_VIOLATIONS=wrong-type,bad-timestamp`),
but the demo script (`../../scripts/demo-violations.mjs`) exercises them one
at a time so each failure in its output is attributable to a single cause.

## Why an in-memory Node server, and not something backend-shaped

The stub's job is to be a moving target for the suite to test itself
against — nothing about its own implementation matters except the shapes
its HTTP responses carry. A zero-dependency Node script is the smallest
thing that can serve those shapes on demand, keeps the whole fixture
auditable in one file, and needs nothing installed beyond the Node already
required to run the suite itself.
