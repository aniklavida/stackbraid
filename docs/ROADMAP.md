# Roadmap to v1.0

One useful, market-ready release, then maintenance driven by real issues and demand.

## 0 · Lock

Positioning, scope, the capability list, the folder structure and the dependency set are agreed and recorded.

**Done:** no unresolved product contradiction remains.

## 1 · Contract and conformance

Write `openapi.yaml` for the `Identity` feature. Build the conformance suite. Wire client generation for TypeScript and Dart.

**Done:** the suite runs against a stub and fails for the right reasons.

## 2 · First backend

One backend end to end: auth, users, roles, the shared plumbing, one database provider, observability, tests.

**Done:** conformance passes; architecture tests pass.

## 3 · Second backend

The same contract, the same folders, the other language.

**Done:** both backends pass the same suite, and the same generated client works against either.

## 4 · Frontends and mobile

Angular and Next.js, each with the public site and the admin area. The Flutter shell.

**Status:** Angular and Next.js are both done — register, sign in, and manage users and roles all work against both backends. The Flutter shell's `Identity` feature (register, sign in, a session surviving a restart, profile, sign out) is implemented and verified against both backends on the macOS desktop target, with no admin surface by design (`docs/STRUCTURE.md`); it has not been verified on an iOS or Android simulator.

**Done:** register, log in and manage users work in every surface against both backends.

## 5 · The professional layer

Background jobs, queue, storage, Excel, PDF, email, audit, caching, rate limiting, realtime, push, localization across every stack.

**Done:** a queued export produces a file and its duration appears on a dashboard; every string is localized in two locales.

## 6 · Remaining providers

SQL Server and MySQL, each with its own migrations.

**Status:** both providers exist on both backends, each with its own migrations, seed data and job store, and the
composition roots select one with `Database:Provider` / `STACKBRAID_DATABASE_PROVIDER`. SQL Server is wired and its
provider tests pass on both backends; Python's SQL Server and MySQL legs run in CI as service containers. The .NET
MySQL leg is blocked: Pomelo 9.0.0 (the only MySQL EF Core provider with an accepted licence) targets EF Core 9 and
cannot build its model on this backend's EF Core 10 baseline — see `docs/DEPENDENCIES.md`.

**Done (not yet):** conformance passes for every backend on every provider.

## 7 · `create`, playbooks and release

The picker, the agent playbooks, documentation, a demo, release automation and clean-install proof.

**Status:** the picker (`create/create.mjs`) copies the chosen backend, database, frontend and mobile pieces and writes fresh, stack-scoped configuration, with no reference to an unchosen stack anywhere in its output — proven for every combination that exists today (see `create/README.md`). One combination per backend has been run natively (no Docker) against a throwaway local Postgres and passed the shared conformance suite. `docker compose up` has not yet been verified for any combination, and the agent playbooks referenced below do not exist yet.

**Done:** a new user can install, run and demonstrate the whole promise without help.

## After v1.0

Maintain compatibility. Fix reproducible bugs and security issues. Add MongoDB. Add features only from repeated user evidence.
