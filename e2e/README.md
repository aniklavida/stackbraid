# e2e — the shared parity script

`identity-flow.ts` holds the one real Playwright test body, used unchanged
by both `frontends/nextjs` and `frontends/angular`. Neither project keeps
its own copy of the test: each one's `e2e/identity-flow.spec.ts` is a
three-line wrapper that imports its own project's `test`/`expect` (so Node
resolves `@playwright/test` from that project's own `node_modules`, the
normal way Playwright's test runner expects) and calls
`registerIdentityFlowSpec(test, expect)` from here. A change to the test
body is a change for both frontends at once.

The spec itself has no frontend- or backend-specific knowledge: it drives
the page through accessible roles, labels and visible text only (`getByRole`,
`getByLabel`), never a CSS class or a component-library implementation
detail. That is what makes the same test body provable against two
frontends built on different UI kits.

## Running it

From either `frontends/nextjs/` or `frontends/angular/`, with that
frontend already built/served and pointed at a running backend:

```bash
PLAYWRIGHT_BASE_URL=http://127.0.0.1:3000 \
FRONTEND_LABEL=nextjs BACKEND_LABEL=dotnet \
SCREENSHOT_DIR=/absolute/path/outside/the/repo \
npm run test:e2e
```

`FRONTEND_LABEL` and `BACKEND_LABEL` only name screenshot files — the test
itself never branches on them. `SCREENSHOT_DIR` must be an absolute path
outside this repository; screenshots are run evidence, not a repo artifact.

## The four-run parity proof

The same test body, run four times — changing only the frontend under test
and the backend it was started against:

| Frontend | Backend |
|---|---|
| Next.js | .NET |
| Next.js | Python |
| Angular | .NET |
| Angular | Python |

Each run needs a real throwaway local Postgres cluster (no Docker — see
either backend's own `scripts/start-local-postgres.sh`), that backend
started and pointed at it, and the frontend under test built/served with
its own base-URL mechanism pointed at that backend (Next.js:
`NEXT_PUBLIC_API_BASE_URL`; Angular: `public/env.js`). See the root
`README.md` and each frontend's own README for the exact commands.
