# Architecture

## System boundary

```
                    contract/openapi.yaml
                             │
        ┌────────────────────┼────────────────────┐
   implements            generates            generates
        │                    │                    │
   BACKENDS             TS client            Dart client
   .NET · Python             │                    │
        │               FRONTENDS              MOBILE
        │            Angular · Next.js         Flutter
        │             web + admin                 │
        └────────────────────┼────────────────────┘
                             │
                  infra — one compose file
        Postgres · RabbitMQ · Redis · Prometheus · Loki · Grafana
```

The contract owns the truth. Backends implement it, clients are generated from it, and the conformance suite proves both.

## Why this is five pieces, not sixteen combinations

A user picks one backend, one database, one frontend and optionally mobile. That is sixteen combinations from their side — and no extra work on ours, because **the frontend never knows which backend serves it.** It knows the contract.

Adding a second frontend does not double the work. It adds one piece that every backend already serves.

## The three layers of the product

1. **Contract** — `openapi.yaml`, plus the realtime message shapes. The single source of truth.
2. **Implementations** — backends that satisfy it, clients generated from it, frontends and mobile built on those clients.
3. **Infrastructure** — one compose file, stack-agnostic, written once and shared by every combination.

Most of the value lives in the third layer, and that is precisely where multiplication is avoided.

## Boundaries

- Core behaviour never depends on one vendor. Authentication lives in the backend; storage, push and realtime are adapters.
- Provider-specific code exists in exactly one folder per backend.
- Secrets stay outside the repository.
- Generated code is committed but never hand-edited.
- Setup and migration are idempotent and preserve user data.

## Localization

Both backends negotiate a locale from the request's `Accept-Language` header with one shared rule: parse it as a comma-separated list of language ranges, each optionally weighted with `;q=`, pick the highest-weighted range this backend actually ships text for (ties keep the header's own order), and fall back to English when nothing matches. A malformed weight is treated as the default rather than dropping that range. Error titles and details returned in the `Problem` envelope are localized server-side from this negotiated locale — never hard-coded English.

Every frontend (Angular, Next.js, Flutter) ships the same locale-switching experience: a language menu that switches instantly and persists the choice for the next visit, defaulting to English. `node scripts/check-translation-keys.mjs` is the completeness gate described above.

## The conformance suite

One suite, run against every backend on every provider. If all pass, every generated client is guaranteed to work against every backend.

Without it, one backend drifts silently and a frontend breaks for reasons nobody can find. It runs in CI on every commit, and it is the single mechanism that keeps a multi-stack skeleton honest.

## Realtime

SignalR in .NET, native WebSockets in Python, Redis backplane in both.

SignalR is .NET-only, so unlike REST there is no shared wire protocol. Realtime **message shapes are therefore part of the contract**, both backends emit identical JSON, and conformance covers the payloads. Only the subscription mechanism differs.

## The agent layer

Procedures live once as tool-neutral markdown playbooks. `AGENTS.md` is read natively by most coding agents; each tool gets a thin pointer rather than a duplicated copy. A human teammate reads the same playbooks, and supporting a new agent tool is one more pointer, not a rewrite.

## Accepted costs

- **Backend features are written twice.** Permanent. Conformance keeps the two honest.
- **Five codebases drift, and upstream frameworks move.** CI is the mitigation and it is not optional.
- **Agent and skill formats evolve**, so the agent layer needs maintenance; a stale agent config is visibly stale.
