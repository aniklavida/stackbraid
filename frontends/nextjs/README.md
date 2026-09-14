# frontends/nextjs

A web app and an admin app in one Next.js project, both built on the
`Identity` feature — register, sign in, view your own profile, and (for an
account holding the `admin` role) manage users and roles. Talks to either
backend through the generated TypeScript client in
[`clients/typescript`](../../clients/typescript) — nothing here writes a
fetch call or a type by hand, and nothing here knows whether it is talking
to `backends/dotnet` or `backends/python`.

**Status: implemented and verified against both backends.** See "Proof" below.

## Structure

Clean Architecture, the same four layers in every feature, enforced by
`dependency-cruiser` (`.dependency-cruiser.mjs`, wired into `npm run
arch:check`):

```
src/
├── app/                    routes only — every file imports from web/ or admin/
│   ├── (web)/               /, /login, /register, /profile
│   └── (admin)/admin/       /admin/users, /admin/users/[userId], /admin/roles
├── shared/                 cross-cutting — no business meaning, never imports a feature
│   ├── auth/                the in-memory session store, AuthProvider, the route guard
│   ├── http/                the one place the generated client is configured
│   ├── i18n/                locale request config, the language switcher
│   └── config/              NEXT_PUBLIC_API_BASE_URL and nothing else
├── web/                    the public site
│   ├── layout/
│   └── features/           auth, home, profile — each domain/ application/ data/ presentation/
└── admin/                  the admin area
    ├── layout/
    └── features/           users, roles — each domain/ application/ data/ presentation/
```

Within a feature: `domain/` depends on nothing (not even the generated
client — see `web/features/profile/domain/profile.ts` for why, and
`admin/features/users/domain/user-filters.ts`); `data/` wraps the generated
client and depends on `domain/`; `application/` orchestrates `domain/` and
`data/`; `presentation/` calls `application/` and `domain/` only, never
`data/` directly. A feature's internals are reachable from outside it only
through that feature's own `index.ts` — the same "only through Contracts"
rule the backends enforce between features, restated for a frontend where a
barrel file is the contract. Every one of these rules was seen to genuinely
fail against a deliberate violation before this file was written; nothing
here documents a rule that was not actually tested.

Localized strings ship with each feature (`presentation/messages/{en,es}.json`),
not in one central file — `shared/i18n/request.ts` is what assembles them
into next-intl's single message tree per request, but each feature still
owns and edits only its own catalogue.

## Running it against a backend

Point it at a running instance of either backend — nothing else changes:

```bash
npm install
NEXT_PUBLIC_API_BASE_URL=http://127.0.0.1:8080 npm run dev
```

The backend needs a session cookie it can actually read across origins:
both backends default to no CORS policy at all, so set the frontend's
origin explicitly when starting one for this app to talk to —
`Cors:AllowedOrigins` (.NET, e.g. `Cors__AllowedOrigins__0=http://127.0.0.1:3000`)
or `STACKBRAID_CORS_ALLOWED_ORIGINS_RAW` (Python, comma-separated). See each
backend's own README for the rest of the startup command (a throwaway local
Postgres cluster, migrations and seeding run automatically).

`npm run dev` and `npm run build` both pass `--webpack` explicitly: this
project links `@stackbraid/client-typescript` from outside
`frontends/nextjs` via `file:../../clients/typescript` (a sibling directory,
not a nested `node_modules` package), and Next.js 16's default bundler
(Turbopack) does not yet resolve a `tsconfig.json` path alias pointing
outside the project root the same way webpack does with
`experimental.externalDir` — confirmed by reproducing the failure with
Turbopack and getting a clean build and a working, hydrating app under
webpack. Re-check this the next time either toolchain updates.

## The one thing a Next.js proxy/middleware file cannot do here

The httpOnly refresh cookie either backend sets is scoped to
`Path=/v1/auth` **on that backend's own origin** — exactly where the
contract says it should be, and exactly where `shared/auth/AuthProvider.tsx`
reads it back via a direct browser fetch. It is never present on a request
to this app's own server, at any path, because the path never matches. A
`middleware.ts`/`proxy.ts` file was tried first and removed once this
became clear — checking for the cookie there always failed, indefinitely
redirecting even a legitimately signed-in administrator away from
`/admin/*`. The route guard (`shared/auth/RequireAuth.tsx`, used by
`admin/layout/AdminShell.tsx` with `requirePermission="users:read"`) is
entirely client-side instead: it redirects once hydration has actually
confirmed (or failed to confirm) a session, and every admin API call is
authoritatively re-checked by the backend's own 403 regardless of what this
component decides first.

## Tests

```bash
npm run typecheck      # tsc --noEmit
npm run lint           # eslint
npm run arch:check     # dependency-cruiser — the layering rules above
npm test               # vitest — domain and application unit tests
npm run test:e2e       # playwright — one Identity-flow happy path
```

The Playwright spec (`e2e/identity-flow.spec.ts`) takes its target from
`PLAYWRIGHT_BASE_URL` and has no backend-specific knowledge, the same way
`contract/conformance` doesn't — it registers a real account, confirms the
session survives a reload, confirms `/admin/*` is unreachable without the
`admin` role, signs back in as the seeded administrator, assigns that role
from the real admin screen, and reads the real roles catalogue. Chromium
only — the point is proving the flow against each backend, not cross-browser
coverage.

## Proof

Run against a throwaway local Postgres cluster (no Docker), each backend's
own server, and this app built and started with `NEXT_PUBLIC_API_BASE_URL`
pointed at it in turn — **identical app, identical Playwright spec, zero
code changes, only the URL differed**:

- `contract/conformance` against `backends/dotnet`: **33 passed, 0 failed, 1 skipped** (the skip is the expiry check, which needs a short-lived access token TTL this run's default didn't set — see the suite's own README).
- `contract/conformance` against `backends/python`: **33 passed, 0 failed, 1 skipped**, same reason.
- `npm run test:e2e` against `backends/dotnet`: **1 passed.**
- `npm run test:e2e` against `backends/python`: **1 passed.**

## Dependencies

See [`docs/DEPENDENCIES.md`](../../docs/DEPENDENCIES.md), "Next.js frontend"
— including one flagged-for-Anik finding (an optional, unused native
dependency `next` itself pulls in) recorded there rather than glossed over.
