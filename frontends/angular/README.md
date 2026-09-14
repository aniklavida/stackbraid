# frontends/angular

A web app and an admin app in one Angular project, built on the same
`Identity` feature as [`frontends/nextjs`](../nextjs) — register, sign in,
view your own profile, and (for an account holding the `admin` role) manage
users and roles. Talks to either backend through the generated TypeScript
client in [`clients/typescript`](../../clients/typescript) — nothing here
writes a fetch call or a type by hand, and nothing here knows whether it is
talking to `backends/dotnet` or `backends/python`.

**Status: implemented and verified against both backends.** See "Proof" below.

Feature names, layering and behaviour match the Next.js app exactly — that
is the parity the two frontends are held to. Layout and idiom are Angular's
own: standalone components, signals for local state, the Router's own
guards, and Angular Material instead of shadcn/ui.

## Structure

Clean Architecture, the same four layers in every feature, enforced by
`dependency-cruiser` (`.dependency-cruiser.mjs`, wired into `npm run arch:check`):

```
src/
├── app/                    routing and bootstrap only — every route lazy-loads from web/ or admin/
│   ├── app.routes.ts        /, /login, /register, /profile, /admin/users, /admin/users/:userId, /admin/roles
│   └── app.config.ts        providers: router, Transloco, TanStack Query
├── shared/                  cross-cutting — no business meaning, never imports a feature
│   ├── auth/                 the token store (a signal), AuthService, the route guards
│   ├── http/                 the one place the generated client is configured
│   ├── i18n/                 the root Transloco loader, the language switcher
│   └── config/               the runtime API base URL and nothing else
├── web/                     the public site
│   ├── layout/                web-shell.ts
│   └── features/             auth, home, profile — each domain/ application/ data/ presentation/
└── admin/                   the admin area
    ├── layout/                admin-shell.ts
    └── features/             users, roles — each domain/ application/ data/ presentation/
```

Within a feature: `domain/` depends on nothing (not even the generated
client — see `web/features/profile/domain/profile.ts` and
`admin/features/users/domain/user-filters.ts`); `data/` wraps the generated
client and depends on `domain/`; `application/` orchestrates `domain/` and
`data/` (TanStack Query wrapped in a small `use*` function per operation,
mirroring the Next.js app's own hooks); `presentation/` calls `application/`
and `domain/` only, never `data/` directly. A feature's internals are
reachable from outside it only through that feature's own `index.ts` — the
same "only through Contracts" rule the backends enforce between features,
restated for a frontend where a barrel file is the contract. Every one of
these rules was seen to genuinely fail against a deliberate violation before
this file was written; nothing here documents a rule that was not actually
tested.

Localized strings ship with each feature (`presentation/messages/{en,es}.json`
or, for a layout, `messages/{en,es}.json`), not in one central file. Each
feature registers its own Transloco scope with an **inline loader** — a
small `<feature>.i18n.ts` next to it — wired onto that feature's route in
`app/app.routes.ts`; `shared/i18n/root-transloco-loader.ts` only ever serves
the cross-cutting chrome (nav labels, the language switcher itself).
Switching language re-renders instantly, with no page reload, no route
prefix and no cookie — `shared/i18n/locale-switcher.ts` persists the choice
to `localStorage` for the next visit.

State is Angular signals throughout — `AuthService` exposes `status`/`user`
as computed signals, every form is a handful of local signals, and server
state goes through `@tanstack/angular-query-experimental`. No NgRx.

## Running it against a backend

Point it at a running instance of either backend by editing
`public/env.js` — **not an environment variable baked in at build time**,
so the exact same compiled build can be repointed without a rebuild:

```js
// public/env.js
window.__STACKBRAID_ENV__ = { apiBaseUrl: "http://127.0.0.1:8080" };
```

```bash
npm install
npm start   # ng serve
```

The backend needs a session cookie it can actually read across origins:
both backends default to no CORS policy at all, so set the frontend's
origin explicitly when starting one for this app to talk to —
`Cors:AllowedOrigins` (.NET, e.g. `Cors__AllowedOrigins__0=http://127.0.0.1:4200`)
or `STACKBRAID_CORS_ALLOWED_ORIGINS_RAW` (Python, comma-separated). See each
backend's own README for the rest of the startup command (a throwaway local
Postgres cluster, migrations and seeding run automatically). The frontend
and the backend must agree on loopback hostname (`127.0.0.1` both sides, or
`localhost` both sides) — the httpOnly refresh cookie's origin match fails
silently otherwise, since a browser treats `localhost` and `127.0.0.1` as
different origins even though both resolve to the same machine.

## The one thing this app does not need that the Next.js one does

The Next.js app's route guard is entirely client-side because a
`middleware.ts` file cannot see the httpOnly refresh cookie — it is scoped
to the *backend's* own origin, never visible to that app's own server at
any path. An Angular route guard has no equivalent server-side stage to
begin with: `shared/auth/auth.guard.ts`'s `authGuard`/`permissionGuard`
simply `await auth.ensureHydrated()` (a promise that resolves once the
initial cookie-based session check has settled) before deciding, so there
is no flash of protected content before a redirect the way a purely
effect-driven check can produce. Every admin API call is still
authoritatively re-checked by the backend's own 403 regardless of what a
guard decides first.

## Tests

```bash
npm run typecheck      # tsc --noEmit
npm run lint           # ng lint (angular-eslint)
npm run arch:check     # dependency-cruiser — the layering rules above
npm test               # vitest (@angular/build:unit-test) — domain unit tests
npm run test:e2e       # playwright — one Identity-flow happy path
```

The Playwright spec is shared with the Next.js app
(`../nextjs/e2e/identity-flow.spec.ts`, driven from this project's own
`playwright.config.ts`) — it takes its target from `PLAYWRIGHT_BASE_URL` and
has no frontend- or backend-specific knowledge, the same way
`contract/conformance` doesn't. It registers a real account, confirms the
session survives a reload, confirms `/admin/*` is unreachable without the
`admin` role, signs back in as the seeded administrator, assigns that role
from the real admin screen, and reads the real roles catalogue. Chromium
only — the point is proving the flow against each frontend and each
backend, not cross-browser coverage.

## Proof

Run against a throwaway local Postgres cluster (no Docker), each backend's
own server, and this app served with `public/env.js` pointed at it in
turn — **identical app, identical Playwright spec, zero code changes, only
the backend URL differed**. See the root `README.md`'s parity section and
Notion cards 12/13 for the full four-run table (this app and the Next.js
app, each against both backends).

## Dependencies

See [`docs/DEPENDENCIES.md`](../../docs/DEPENDENCIES.md), "Angular frontend".
