# StackBraid — product specification

**Status:** Draft. Nothing in this document is implemented yet.

## 1 · What it is

A professional skeleton for building a real product. You choose your stack — backend, database, frontend, mobile — and receive working, already-connected codebases with everything a serious project needs already wired. You write business logic. Nothing else.

## 2 · The boundary of "ready"

> **Ready = what is the same in every product. Empty = what differs between products.**

Auth is identical in every product ever built, so it ships. Invoices are not, so they do not.

That line has a consequence: **StackBraid ships exactly one feature, `Identity`.** No sample business domain for a user to delete.

## 3 · Who it is for

Engineers and AI-assisted developers starting a new product. Small teams and agencies who already maintain a private house skeleton and would rather use one good public one.

## 4 · The problem

Starting a serious product costs a week of wiring before the first feature: authentication, migrations, background jobs, a message queue, observability, localization, an admin panel, a mobile client that matches the API.

Each of those is a solved problem on its own. The combination is not, and every team solves it again from scratch.

## 5 · Four independent picks

| Pick | Options |
|---|---|
| Backend | **.NET** · **Python** |
| Database | **PostgreSQL** · **SQL Server** · **MySQL** · *(MongoDB after the SQL providers)* |
| Frontend | **Angular** · **Next.js** — each containing the public site **and** the admin area |
| Mobile | **Flutter** · none |

Nobody takes two backends. Because every piece implements the same contract, **this is five independent pieces, not sixteen combinations** — the frontend never knows which backend serves it.

## 6 · Capabilities

### Essential

Auth (register, login, JWT with refresh, roles, permissions) · user and role management with admin screens · **localization, including locale-aware backend error messages** · a standard response and error-code envelope · validation surfaced correctly in every client · pagination, filtering and sorting conventions · migrations and seeding · configuration and secret management · structured logging with correlation IDs · health and readiness endpoints · Docker Compose for the whole system · CI running build, test, lint, architecture and conformance · **the agent layer**.

### Professional

Background jobs · RabbitMQ · file storage (local and S3-compatible) · Excel import with row-level error reporting, and export · PDF generation · templated email · audit log · soft delete and restore · rate limiting · Redis caching · **API versioning from day one** · timezone and date rules decided once · **realtime** · **push notifications** · the full test matrix · a Flutter shell with auth, navigation and offline-aware HTTP.

### Not in v1

Multi-tenancy · feature flags · third-party SSO providers · full-text search · webhooks.

Multi-tenancy is excluded deliberately and honestly: it touches every table and every query, and "easy to add later" would not be true. If it is wanted, it has to enter at the first commit.

## 7 · Architecture

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

**Contract-first.** `openapi.yaml` is hand-written and authoritative; every backend implements it and every client is generated from it. With two backends, code-first would be incoherent — it would silently make one backend the truth and leave the other trailing.

## 8 · The conformance suite

One suite, written against the contract, run against every backend on every database provider.

```
contract/conformance/  →  .NET + PostgreSQL     ✅
                       →  Python + PostgreSQL   ✅
                       →  .NET + MySQL          ✅   …
```

If every backend passes, every generated client is guaranteed to work against every backend. Without it, one backend drifts silently and a frontend breaks for reasons nobody can find.

**This is the single mechanism that keeps a multi-stack skeleton honest**, and it runs in CI on every commit.

## 9 · The agent layer

Tool-neutral by construction. Procedures live once as plain markdown; each tool gets a thin pointer rather than a duplicated copy.

```
AGENTS.md                     read natively by most coding agents
CLAUDE.md · GEMINI.md         thin — they point at AGENTS.md
.agent/playbooks/             the source
.claude/skills/ · .cursor/    pointers
```

**Playbooks that ship:** `add-feature` · `add-endpoint` · `add-entity` · `add-migration` · `regenerate-clients` · `add-background-job` · `add-localized-string` · `run-conformance` · `design-screen` · `add-admin-screen` · `add-web-page` · `make-accessible` · `review-architecture` · `review-dependency`.

Only the playbooks matching the chosen stacks are installed — an agent must never give instructions for code that is not in the project.

**Why this is essential rather than a nice-to-have.** A skeleton's real failure mode is people abandoning its conventions: month three, someone adds an endpoint by hand, skips the contract, hand-writes a client, and it rots from inside. StackBraid has exactly one correct path, which is precisely what a playbook can encode.

## 10 · `create`

```
npx stackbraid create acme
? Backend   › .NET
? Database  › PostgreSQL
? Frontend  › Angular
? Mobile    › Flutter
? Infra     › Postgres, RabbitMQ, Redis, Prometheus + Grafana
```

It copies the chosen folders and writes configuration. **It is not a code generator** — the code you receive is exactly the code in this repository, idiomatic and readable. A .NET engineer opens a .NET-shaped project.

It ships first as an in-repo script requiring no npm publish, and becomes `npx stackbraid create` at v1.

## 11 · Dependency policy

**Every dependency shipped here is inherited by every user.** Each one is licence-audited before it enters.

| Class | Example | Rule |
|---|---|---|
| Compiled into user code | a mediator, an Excel library | **MIT / Apache / BSD only** |
| Run as a separate process | Postgres, RabbitMQ, Grafana | Copyleft acceptable — it never reaches user code |

| Concern | Choice | Licence |
|---|---|---|
| Dispatch (.NET) | `martinothamar/Mediator` | MIT |
| Dispatch (Python) | container-resolved handlers | no dependency |
| Mapping | **hand-written** | — |
| Validation | FluentValidation | Apache-2.0 |
| Background jobs | Postgres-backed scheduler (`PersistentJobScheduler`, no new dependency); Hangfire not adopted (LGPL compiled into user code) | MIT |
| PDF | QuestPDF | Community — free under USD 1M revenue |
| Excel | ClosedXML | MIT |
| Logging | Serilog | Apache-2.0 |
| Observability | OpenTelemetry → Prometheus · Loki · Grafana | Apache-2.0 |
| UI (Angular) | Angular Material | MIT |
| UI (Next.js) | shadcn/ui + Tailwind | MIT |
| Realtime | SignalR · FastAPI WebSockets | MIT |
| Push | Firebase Cloud Messaging | BSD-3 |
| MySQL driver | **Pomelo**, never `MySql.Data` | MIT (the latter is GPL-2.0) |

**Rejected:** MediatR and AutoMapper (RPL 1.5 or paid) · FluentAssertions v8+ (non-commercial only) · EPPlus (paid for commercial use) · `MySql.Data` (GPL-2.0).

PDF generation and job scheduling sit behind `IPdfGenerator` and `IJobScheduler`, so a user outside QuestPDF's free tier swaps one class.

**Token storage:** an httpOnly refresh cookie plus an access token held in memory. Browser local storage is simpler and is what most tutorials do, but it is readable by any cross-site scripting flaw — and a skeleton inherited by many projects should demonstrate the safe pattern.

## 12 · Realtime

**SignalR** in .NET, **native FastAPI WebSockets** in Python, with a Redis backplane in both so it scales past one instance.

One asymmetry stated honestly: SignalR is .NET-only, so unlike REST there is no shared wire protocol. **Realtime message shapes are therefore defined in the contract** alongside the REST schemas, and both backends emit identical JSON. A client subscribes differently per backend; what it receives is the same, and conformance covers the payloads.

The shipped surface is a notification stream and a job-progress channel — enough to prove the plumbing, not a chat product.

## 13 · API versioning

`/v1/` from the first commit. Routes, contract and generated clients all carry it. Retrofitting versioning into a contract users have already built against is painful and public; starting with it costs one path segment.

## 14 · Notifications and vendor adapters

Behind interfaces, configured rather than compiled in:

| Interface | Shipped implementations |
|---|---|
| `INotificationSender` | Firebase Cloud Messaging · email · in-app |
| `IFileStorage` | local · S3-compatible · Supabase |
| `IRealtimePublisher` | SignalR · FastAPI WebSockets |

Authentication deliberately has no vendor adapter. It lives in the backend so no user is forced into an external identity provider.

## 15 · Tests

Every suite is wired and runnable from the first commit. A user deletes what they do not want rather than building what they do.

| Stack | Unit | Integration | E2E | Architecture |
|---|---|---|---|---|
| .NET | xUnit · Shouldly · NSubstitute | Testcontainers | — | NetArchTest |
| Python | pytest · pytest-asyncio | Testcontainers | — | import-linter |
| Angular | Vitest | — | Playwright | dependency-cruiser |
| Next.js | Vitest · Testing Library | — | Playwright | dependency-cruiser |
| Flutter | `flutter_test` | `integration_test` | — | — |
| Contract | — | — | — | conformance, every backend × provider |

**End-to-end stays at one happy path per surface.** The skeleton demonstrates the shape; a large e2e suite a user did not write becomes maintenance they did not ask for.

## 16 · MongoDB

MongoDB is a **second data paradigm, not a fourth provider.** There are no migrations, modelling is embedded rather than normalised, and each language needs a different persistence stack rather than a swapped driver.

**Approach: design for it now, ship it after the SQL providers work.** From the first commit, repository interfaces stay free of SQL assumptions — nothing query-provider-specific leaking through, no migration assumptions in shared persistence. That costs nothing now and makes MongoDB real later.

## 17 · Non-goals

Not a code generator · not a framework · not a hosted service · **not a feature shipped in one stack only**.

## 18 · v1 acceptance

- [ ] `create` produces a working project for every combination, containing only the chosen pieces.
- [ ] `docker compose up` brings up the full system with no manual steps.
- [ ] Register, log in and manage users and roles work in web, admin and mobile against **both** backends.
- [ ] The conformance suite passes for every backend on every shipped provider, in CI.
- [ ] Generated clients are byte-identical to a fresh regeneration; drift fails the commit.
- [ ] Architecture tests pass in both backends.
- [ ] An Excel import reports row-level errors; an export and a PDF are produced by a queued job whose duration appears on a dashboard.
- [ ] A realtime notification and a job-progress update arrive in web, admin and mobile from both backends.
- [ ] Every route, contract path and generated client carries `/v1/`.
- [ ] Every string is localized, backend error messages included, in at least two locales.
- [ ] All five test suites run green from a clean checkout, in CI.
- [ ] Adding a feature via the `add-feature` playbook works in a clean checkout, for more than one agent tool.
- [ ] Every shipped dependency passes the licence audit and is listed with its licence.
- [ ] Every README claim has working evidence or is labelled planned.

## 19 · Risks

- **Maintenance is the real cost.** Five codebases drift and upstream frameworks move. A stale skeleton is worse than none. The conformance suite and CI are the mitigation, and they are not optional.
- **Two backends means backend features are written twice.** That cost is permanent and accepted; conformance is what keeps the two honest.
- **Breadth can outrun depth.** Shipping five pieces badly is worse than three well. Acceptance criteria are binary for this reason.
