# StackBraid TypeScript client

Generated from `contract/openapi.yaml` by [`@hey-api/openapi-ts`](https://heyapi.dev)
(MIT). **Never hand-edit anything under `src/generated/`** — regenerate it
instead. A hand-edit is exactly what the drift check (`scripts/check-client-drift.sh`
at the repository root) exists to catch, and the pre-commit hook runs it.

**Status:** compiles (`tsc --noEmit` passes). It has never called a real
server — no backend exists yet (see `docs/SPEC.md`). Generating a client is
not the same claim as a working integration.

## Regenerate

From the repository root, regenerate both clients with one command:

```bash
./scripts/generate-clients.sh
```

Or just this one, from this directory:

```bash
npm install
npm run generate
```

## Verify

```bash
npm run typecheck   # tsc --noEmit
```

## Using it

The generated client is self-contained — `@hey-api/openapi-ts` bundles its
fetch-based runtime directly into `src/generated/client/`, so there are no
runtime dependencies to install beyond what a modern Node.js or browser
already provides.

```ts
import { client, listUsers } from './src/generated';

client.setConfig({ baseUrl: 'https://your-backend-host' });

const { data } = await listUsers({ query: { page: 1, pageSize: 20 } });
```
