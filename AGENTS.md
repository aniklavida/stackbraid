# StackBraid — contributor and agent instructions

The canonical guide for humans and coding agents working in this repository. Tool-neutral: Claude, Codex, Cursor, Gemini CLI and others read this file.

## What this repository is

A multi-stack product skeleton. Users pick a backend, a database, a frontend and optionally mobile, and receive working codebases wired together by one OpenAPI contract.

**Status: early implementation.** The specification, architecture and structure exist, both backends' `Identity` feature is implemented, tested and passes contract conformance (see `backends/dotnet/README.md` and `backends/python/README.md`), and both the Next.js and Angular frontends' web and admin shells are implemented and verified against both backends, using the same Playwright script (see `frontends/nextjs/README.md` and `frontends/angular/README.md`). The Flutter mobile shell's `Identity` feature is implemented and verified against both backends on the macOS desktop target (see `mobile/flutter/README.md`); it has no admin surface by design and has not been verified on an iOS or Android simulator. Remaining database providers are not built yet.

## The one rule that matters

**The contract comes first.**

```
contract/openapi.yaml   →   backends implement it
                        →   clients are generated from it
                        →   conformance proves both
```

Never change an API by editing a backend. Change `contract/openapi.yaml`, regenerate the clients, then implement. A hand-edited client will be overwritten and will fail the drift check.

## Adding anything

1. Edit `contract/openapi.yaml`.
2. Regenerate the TypeScript and Dart clients.
3. Implement in the backend you are changing — and the other one.
4. Implement in the frontend and mobile if the feature has a surface.
5. Run the conformance suite against every backend.
6. Add or update localized strings for every locale.

**A feature that cannot be done end-to-end in every shipped stack does not go in.**

## Structure

```
src/
├── Shared/       cross-cutting plumbing — no business meaning
├── Database/     provider-specific; the only place a provider name appears
├── Features/     one folder per domain area, the SAME NAME in every stack
└── Host/         composition root
```

Every feature carries four layers, in every stack:

| Layer | Backend | Frontend and mobile |
|---|---|---|
| Models and rules | `Domain/` | `domain/` |
| Use cases | `Application/` | `application/` |
| Data access | `Persistence/` | `data/` |
| Delivery | `Endpoints/` | `presentation/` |

Two rules, both enforced by tests in CI:

- **A feature never reaches into another feature's internals** — only its `Contracts`.
- **`Shared` never imports a feature.** The moment it does, it stops being shared.

## Naming

Feature names are identical across every stack. Adding `invoices` means a folder called `invoices` in every piece — not `Invoice`, not `billing`. Casing follows each language's convention; the word does not change.

## Dependencies

**Every dependency shipped here is inherited by every user of this skeleton.** Audit the licence before adding one.

| Class | Rule |
|---|---|
| Compiled into user code | MIT, Apache or BSD only |
| Run as a separate process | Copyleft acceptable — it never reaches user code |

Anything reciprocal-for-consumers — RPL, SSPL, RSAL, BSL, or a revenue-gated commercial licence — is rejected regardless of quality.

**Currently rejected:** MediatR and AutoMapper (RPL 1.5 or paid) · FluentAssertions v8+ (non-commercial only) · EPPlus (paid for commercial use) · `MySql.Data` (GPL-2.0 — use Pomelo).

## Truthfulness

Every public claim is one of: **implemented and tested**, **experimental**, **planned**, or **unsupported**. Never describe a planned capability as working. If you cannot demonstrate it, label it planned.

## Generated code

`clients/` and every `api/` folder are generated and committed. **Never hand-edit them.** Regenerate from the contract instead; a drift check fails the commit.

## Tests

Unit, integration, end-to-end, architecture and contract conformance. All five run in CI. Run conformance against every backend before claiming a change works.

## Playbooks

Standard procedures live as tool-neutral playbooks in [`.agent/playbooks/`](.agent/playbooks/):

- [`add-feature`](.agent/playbooks/add-feature.md) — end-to-end flow for introducing a new domain feature slice across all five stacks
- [`add-endpoint`](.agent/playbooks/add-endpoint.md) — contract-first endpoint addition: OpenAPI schema, client regeneration, dual backend implementation, and conformance
- [`add-entity`](.agent/playbooks/add-entity.md) — entity modeling in Domain and provider-agnostic Persistence configuration
- [`add-migration`](.agent/playbooks/add-migration.md) — generating and applying provider-specific migrations across Postgres, SQL Server, and MySQL
- [`regenerate-clients`](.agent/playbooks/regenerate-clients.md) — client regeneration from the contract and verifying zero drift
- [`add-background-job`](.agent/playbooks/add-background-job.md) — registering durable background jobs and handlers across both backends
- [`add-localized-string`](.agent/playbooks/add-localized-string.md) — adding translated strings to all catalogues and verifying completeness
- [`run-conformance`](.agent/playbooks/run-conformance.md) — executing the zero-dependency contract conformance test suite against any running backend
- [`design-screen`](.agent/playbooks/design-screen.md) — UI design rules, layout boundaries, design tokens, and web/admin separation
- [`add-admin-screen`](.agent/playbooks/add-admin-screen.md) — building administrative management screens in Angular and Next.js admin shells
- [`add-web-page`](.agent/playbooks/add-web-page.md) — creating public/authenticated web pages in Angular and Next.js web shells
- [`make-accessible`](.agent/playbooks/make-accessible.md) — WCAG 2.1 AA accessibility guidelines, semantic HTML, ARIA, and automated axe checks
- [`review-architecture`](.agent/playbooks/review-architecture.md) — verifying layering rules, import boundaries, and running architecture test suites
- [`review-dependency`](.agent/playbooks/review-dependency.md) — licence auditing policy, dependency classification, inventory updates, and past precedent
