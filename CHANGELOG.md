# Changelog

All notable changes to StackBraid are documented here, following [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Product specification, architecture, folder structure, roadmap and release checklist.
- Contributor and agent instructions.
- The `Identity` API contract (`contract/openapi.yaml`): auth, users and roles, validated against OpenAPI 3.1.
- A conformance suite (`contract/conformance/`), written against the contract and runnable against any backend on any database provider, with a stub fixture proving it catches contract violations.
- Generated TypeScript (`clients/typescript/`) and Dart (`clients/dart/`) clients, wired to regenerate from `contract/openapi.yaml` with one command (`scripts/generate-clients.sh`), plus a drift check and pre-commit hook that fail when the committed clients no longer match a fresh generation. Both clients have since called a real running .NET backend successfully, with no hand edits.
- The .NET backend's `Identity` feature (`backends/dotnet`): `Shared` cross-cutting plumbing (a provider-agnostic persistence base, the RFC 9457 Problem envelope, correlation IDs, rate limiting, localization in English and Spanish, password and refresh-token hashing, and one minimal working implementation each for messaging, jobs, documents, caching, storage and mailing); the Identity domain (`User`, `Role`, `RefreshToken`, five domain events, genuinely zero dependencies); a Postgres provider (migrations, idempotent seeding); the full Application layer (one command or query per contract operation); and the HTTP surface — JWT-authenticated, permission-gated minimal API endpoints. Proven against a real, throwaway local Postgres cluster (no Docker, no Testcontainers) and against `contract/conformance` itself: 34 of 34 checks pass. 115 unit and integration tests (32 + 65 + 18 across three test projects), all passing.
- The repository folder scaffold from `docs/STRUCTURE.md` — `frontends/`, `mobile/`, `create/`, and the Python half of `backends/` — each a placeholder stating plainly that no code lives there yet.
- `infra/compose.yaml`: Postgres, RabbitMQ, Redis, Prometheus, Loki and Grafana on one network, every value defaulted so the stack runs with no `.env` file. Prometheus scrapes itself and RabbitMQ's built-in metrics plugin; Grafana auto-provisions both datasources and three pre-built dashboards (`infra/grafana/dashboards/`). `.env.example` names every variable the compose file reads and fills none in.
- CI (`.github/workflows/ci.yml`): lint (shellcheck, yamllint, dashboard JSON validation), build (compose config validation, TypeScript client typecheck), test (the Dart client's test suite), and a `client-drift` job that runs `scripts/check-client-drift.sh` in CI so drifted clients can no longer reach `main` through an unconfigured clone or a `--no-verify` commit.
- A dependency licence audit (`docs/DEPENDENCIES.md`, `docs/dependency-inventory.json`): every npm dependency in `clients/typescript/`, every Dart dependency in `clients/dart/`, and every Docker image in `infra/compose.yaml`, verified against actual package and licence metadata rather than carried forward from a prior note, with the date each entry was checked. All dependencies compiled into a consumer's code are MIT or BSD-3-Clause. No third-party code is vendored or redistributed today, so no `THIRD_PARTY_NOTICES` file is required — that determination is recorded in writing in `docs/DEPENDENCIES.md`. A CI job (`scripts/check-dependency-licenses.mjs`) now fails the build if a dependency is added, or changes version, without a matching audited entry.

The .NET backend's `Identity` feature is implemented and tested; nothing else is. There is no release. The infrastructure stack has not been run end to end on a clean machine — see the commit messages above for exactly what was and was not verified.
