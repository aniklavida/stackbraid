# StackBraid

**One contract. Every app.**

StackBraid is a professional skeleton for building a real product. You choose your stack — backend, database, frontend, mobile — and get working, already-connected codebases with the plumbing a serious project needs already wired. You write business logic. Nothing else.

> **Pre-implementation.** This repository currently contains the product specification, architecture and structure. **There is no working release yet.** Every capability below is planned unless explicitly marked implemented.

## The idea

Starting a serious product costs a week of wiring before the first feature: auth, migrations, background jobs, a queue, observability, localization, an admin panel, a mobile client that matches the API. Each of those is solved in isolation. The combination is not, and every team re-solves it.

StackBraid is that combination, maintained once.

## What you pick

| | Options |
|---|---|
| **Backend** | .NET · Python |
| **Database** | PostgreSQL · SQL Server · MySQL |
| **Frontend** | Angular · Next.js — each containing both the public site and the admin area |
| **Mobile** | Flutter · none |

Nobody takes two backends. Because every piece implements the same OpenAPI contract, **the frontend never knows which backend serves it** — so these are five independent pieces, not sixteen combinations to maintain.

## The boundary

> **Ready = what is the same in every product. Empty = what differs between products.**

Auth is identical in every product ever built, so it ships. Invoices are not, so they don't.

A consequence worth stating plainly: **StackBraid ships exactly one feature — `Identity`.** No sample business domain for you to delete.

## What comes wired

**Essential** — auth with roles and refresh tokens · user and role management with admin screens · localization including locale-aware backend errors · a standard response and error envelope · validation surfaced in every client · pagination and filtering conventions · migrations and seeding · configuration and secrets · structured logging with correlation IDs · health endpoints · Docker Compose · CI · the agent layer.

**Professional** — background jobs · RabbitMQ · file storage · Excel import with row-level errors, and export · PDF generation · templated email · audit log · soft delete · rate limiting · Redis caching · API versioning · realtime · push notifications · the full test matrix.

## The agent layer

StackBraid ships its own agent configuration, and it is **tool-neutral**. Procedures live once as plain markdown playbooks; `AGENTS.md` is read natively by most coding agents, and each tool gets a thin pointer rather than a duplicated copy.

A skeleton's real failure mode is people abandoning its conventions — month three, someone adds an endpoint by hand, skips the contract, hand-writes a client, and it rots from inside. StackBraid has exactly one correct path, which is what a playbook can encode.

## Dependencies

**Every dependency shipped here is inherited by every user of this skeleton.** Full audit, every licence, every verification date: [`docs/DEPENDENCIES.md`](docs/DEPENDENCIES.md). Direct, top-level dependencies of what exists today:

| Package | Licence | Class |
|---|---|---|
| `@hey-api/openapi-ts` (TypeScript client) | MIT | build-tooling |
| `typescript` (TypeScript client) | Apache-2.0 | build-tooling |
| `dio` (Dart client) | MIT | compiled into user code |
| `copy_with_extension` (Dart client) | MIT | compiled into user code |
| `json_annotation` (Dart client) | BSD-3-Clause | compiled into user code |
| `build_runner`, `copy_with_extension_gen`, `json_serializable`, `test` (Dart client, dev-only) | BSD-3-Clause / MIT | build-tooling |
| `Mediator.Abstractions` (.NET backend) | MIT | compiled into user code |
| `FluentValidation` (.NET backend) | Apache-2.0 | compiled into user code |
| `Microsoft.EntityFrameworkCore` (.NET backend) | MIT | compiled into user code |
| `xunit`, `Shouldly`, `NSubstitute` (.NET backend, test-only) | Apache-2.0 / BSD-3-Clause | build-tooling |
| `postgres:16-alpine` | PostgreSQL Licence | separate process |
| `rabbitmq:3.13-management-alpine` | MPL-2.0 | separate process |
| `redis:7.2-alpine` | BSD-3-Clause | separate process |
| `prom/prometheus:v2.54.1` | Apache-2.0 | separate process |
| `grafana/loki:2.9.8` | AGPL-3.0-only | separate process |
| `grafana/grafana:11.2.0` | AGPL-3.0-only | separate process |

A CI check (`scripts/check-dependency-licenses.mjs`) fails the build if a dependency changes version or is added without a matching entry in the audit.

## Documentation

- [Product specification](docs/SPEC.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Folder structure](docs/STRUCTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Release checklist](docs/RELEASE_CHECKLIST.md)
- [Dependency licence audit](docs/DEPENDENCIES.md)

## Licence

MIT. See [LICENSE](LICENSE).
