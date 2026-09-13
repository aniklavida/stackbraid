# The contract

`openapi.yaml` is hand-written and authoritative. Both backends (.NET, Python)
implement it exactly; every generated client (TypeScript, Dart) is generated
from it, never the other way round. See `docs/SPEC.md` for why this project
is contract-first at all, and `docs/ARCHITECTURE.md` for where the contract
sits relative to everything else.

**Status:** the contract for `Identity` exists. No backend implements it yet
— nothing here is running code. Do not read this file as evidence the API
works; read it as the specification a backend will be judged against.

## Changing it safely

1. **Edit `openapi.yaml` first, always.** An API never changes by editing a
   backend and letting the contract drift to match. If a backend needs a
   field the contract doesn't have, the contract gets the field, in its own
   commit, before the backend change.
2. **Validate before committing:**

   ```bash
   npx @redocly/cli lint contract/openapi.yaml
   ```

   Redocly's `lint` understands OpenAPI 3.1 and is what this contract was
   validated against (see below). A commit that fails lint does not go in.
3. **Small, reviewable diffs.** Add one endpoint or one schema at a time
   where the change allows it. A reviewer should be able to tell what
   changed and why from the diff alone.
4. **Every endpoint keeps at least one documented error response.** A
   contract that only describes the happy path teaches a client to only
   handle the happy path.
5. **Regenerate, don't hand-edit, clients.** `clients/typescript` and
   `clients/dart` are generated from this file by
   `./scripts/generate-clients.sh` and committed — never hand-edited. The
   same rule `STRUCTURE.md` states for the rest of the repo applies: an
   `api/` folder is generated and never hand-edited. If a generated client
   ever needs a change that isn't in the contract, that's a sign the
   contract is wrong, not the client. A pre-commit hook
   (`scripts/check-client-drift.sh`, enabled by `git config core.hooksPath
   .githooks`) fails the commit if the committed clients drift from a fresh
   regeneration.
6. **Breaking changes get a new major path prefix, not a silent edit.**
   `/v1/` is in the path from the first commit specifically so this is
   possible later without breaking every existing client at once. Nothing in
   `Identity` has shipped, so nothing is breaking yet — but the rule starts
   now, not once it's inconvenient.

## Two decisions this file locks

Every client in every stack inherits both of these. They are recorded here,
not just in a commit message, because a schema file is read far more often
than its history.

### The error envelope

Every non-2xx response is [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)
Problem Details, served as `application/problem+json`, extended with three
fields:

| Field | Purpose |
|---|---|
| `code` | Stable, machine-readable, e.g. `IDENTITY.INVALID_CREDENTIALS`. What a client branches on. Stable across locales — `title` and `detail` are localized text and will change per request; `code` never does. |
| `traceId` | Correlation ID, matching structured logs, so a bug report can be traced back to a server-side log line. |
| `errors` | Present only on validation failures (`status: 400`) — a field name mapped to its violation messages. |

**Why RFC 9457 instead of inventing an envelope:** both backends need to
produce byte-for-byte identical error bodies despite ASP.NET Core and FastAPI
having completely different default error shapes. ASP.NET Core's
`ProblemDetails` type already speaks this format natively; FastAPI needs a
custom exception handler to match it, which is a small, one-time cost against
inventing and then explaining a bespoke shape in every SDK forever. RFC 9457
also isn't foreign to frontend or mobile developers picking a stack — it's
the same envelope `dotnet new webapi` already produces.

### Pagination: offset, not cursor

`GET /v1/users` takes `page` and `pageSize` and returns:

```json
{ "page": 1, "pageSize": 20, "totalItems": 134, "totalPages": 7, "items": [ ... ] }
```

realized in the contract as `Page` (the shared shape) plus `UserPage`
(`Page` + a typed `items` array) — OpenAPI 3.1 has no generics, so `Page<T>`
becomes one concrete schema per list endpoint rather than a single
parameterized one. As more paginated lists arrive, each gets its own
`<Thing>Page` schema built the same way.

**Why offset over cursor**, given both are legitimate: this is an admin
table (`STRUCTURE.md`'s `admin/features/users`), and admin tables want page
numbers, a total count, and the ability to jump to page 6 — all of which a
cursor makes awkward or impossible. Cursor pagination earns its complexity
on high-write, append-mostly feeds where a page number would drift under the
reader (a social timeline, an activity log); the user list is comparatively
low-write and small enough per page that "how many pages are there"
matters more than "did a row shift under me between requests." If a future
feature is genuinely feed-shaped, it gets cursor pagination on its own
merits — this decision covers `Identity`'s lists, not every list StackBraid
will ever have.

## Realtime shapes

SignalR (.NET) and native WebSockets (Python) don't share a wire protocol —
that's an acknowledged asymmetry, not an oversight (`docs/SPEC.md` §12). What
they must share is the JSON they emit, so those shapes — `RealtimeMessage`
and its variants — are defined in `openapi.yaml` next to the REST schemas
instead of in a separate document that could quietly drift from them.
OpenAPI itself has no concept of a push channel, so the two channels and
which backend endpoint serves each are documented under the `x-realtime-channels`
vendor extension at the bottom of the file — informational only, not
machine-verified today. The conformance suite (a later card) is what will
actually check both backends emit identical payloads.

## Validated against

`openapi.yaml` was checked with two independent tools, not one, because a
linter that only checks its own opinions about style is not the same claim
as a validator checking the document is structurally valid OpenAPI 3.1:

```bash
npx @redocly/cli@2.52.1 lint contract/openapi.yaml
# 0 errors, 2 warnings — both read and accepted, see below

python -m openapi_spec_validator contract/openapi.yaml
# contract/openapi.yaml: OK
```

The two Redocly warnings are deliberate, not overlooked:

- `no-server-example.com` — the `servers` entry is `http://localhost:8080`
  with a description stating plainly that no backend exists yet. Pointing it
  at a real host would be a claim this repository doesn't get to make (see
  the root `AGENTS.md` truthfulness rule); pointing it at nothing was worse
  for anyone opening this file in a viewer.
- `no-unused-components` on `RealtimeMessage` — it's referenced from
  `x-realtime-channels`, a vendor extension Redocly's usage-checker doesn't
  walk. It is not actually unused; it's exactly what §"Realtime shapes"
  above describes.
