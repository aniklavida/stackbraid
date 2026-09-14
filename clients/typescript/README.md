# StackBraid TypeScript client

Generated from `contract/openapi.yaml` by [`@hey-api/openapi-ts`](https://heyapi.dev)
(MIT). **Never hand-edit anything under `src/generated/`** — regenerate it
instead. A hand-edit is exactly what the drift check (`scripts/check-client-drift.sh`
at the repository root) exists to catch, and the pre-commit hook runs it.

**Status:** compiles (`tsc --noEmit` passes) and has called both real
backends successfully (see `backends/dotnet/README.md` and
`backends/python/README.md` for the conformance evidence).

## Realtime — `src/realtime.ts`

The one hand-written file in this package. OpenAPI has no concept of a
push channel, so there is nothing for the generator to produce for
`contract/openapi.yaml`'s `x-realtime-channels` section — this module
connects to whichever transport the target backend actually speaks
(SignalR for .NET, a native WebSocket for Python, auto-detected) and hands
back every `RealtimeMessage`, a type still imported from the generated
output rather than redeclared here. Import it via the package's `./realtime`
subpath:

```ts
import { connectRealtimeChannel } from '@stackbraid/client-typescript/realtime';

const connection = await connectRealtimeChannel(baseUrl, 'notifications', accessToken);
connection.onMessage((message) => {
  if (message.type === 'user.deactivated') {
    // ...
  }
});
```

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
