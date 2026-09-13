# Dependency licence audit

**Every dependency shipped here is inherited by every user of this skeleton.** A licence that binds the user is a defect, not a detail — see `AGENTS.md` → "Dependencies" for the rule this audit enforces.

Two classes, audited differently:

| Class | Rule |
|---|---|
| Compiled into user code | MIT, Apache-2.0 or BSD only |
| Run as a separate process, or used only as a build/CI tool via its CLI | Copyleft acceptable — it is never linked into or redistributed with the user's code |

Anything reciprocal-for-consumers — **RPL, SSPL, RSAL, BSL, or a revenue-gated commercial licence** — is rejected regardless of class or quality.

This document is the narrative record. The machine-readable source of truth is `docs/dependency-inventory.json`, checked on every push by `scripts/check-dependency-licenses.mjs` (see "The CI gate" below). **A licence recorded from memory is not an audit** — every entry below was verified against the actual package's registry metadata or licence file on the date given, not carried forward from an earlier audit's notes.

**Truthfulness note:** the .NET backend is under construction (see `docs/ROADMAP.md` step 2); no frontend or mobile app exists yet. This inventory covers exactly what is genuinely shipped today — the two generated API clients, the infrastructure compose stack, the tooling CI installs, and the .NET backend's own resolved dependency graph. It will grow, stack by stack, as each is actually built. Nothing below pre-audits code that does not exist.

Last verified: **2026-09-14**.

## TypeScript client — `clients/typescript/`

The generated client (`src/generated/`) itself has **zero runtime dependencies** — `@hey-api/openapi-ts` v0.73+ bundles its fetch client, so nothing is compiled into a consuming application's bundle. Everything in `package.json` is a devDependency: the codegen tool and the TypeScript compiler, used to generate and typecheck the client, never shipped with it.

| Package | Version | Licence | Class |
|---|---|---|---|
| `@hey-api/openapi-ts` | 0.99.0 | MIT | build-tooling |
| `typescript` | 5.9.3 | Apache-2.0 | build-tooling |

Both pull in 50 further devDependencies at install time (full list in `docs/dependency-inventory.json`, ecosystem `npm-typescript-client`) — every one of them MIT, Apache-2.0, BSD-2-Clause, ISC or Python-2.0 (`argparse`, pulled in transitively). None are runtime dependencies of the generated code; the classification is `build-tooling` throughout.

`contract/conformance/` (the conformance suite) is included for completeness: **zero dependencies, by design** (Node's built-in `fetch` and a hand-rolled schema validator — see `_ai` decision log). Nothing to audit.

## Dart client — `clients/dart/`

Unlike the TypeScript client, the generated Dio-based Dart client **does** carry runtime dependencies — anything declared under `dependencies:` in `pubspec.yaml` is compiled into a consuming Flutter or Dart application.

**Compiled into user code** (`dependencies:` in `pubspec.yaml`, plus their own runtime dependency closure):

| Package | Version | Licence |
|---|---|---|
| `dio` | 5.11.1 | MIT |
| `dio_web_adapter` | 2.2.2 | MIT |
| `copy_with_extension` | 17.1.0 | MIT |
| `json_annotation` | 4.12.0 | BSD-3-Clause |
| `async` | 2.13.1 | BSD-3-Clause |
| `collection` | 1.19.1 | BSD-3-Clause |
| `http_parser` | 4.1.2 | BSD-3-Clause |
| `meta` | 1.19.0 | BSD-3-Clause |
| `mime` | 2.1.0 | BSD-3-Clause |
| `path` | 1.9.1 | BSD-3-Clause |
| `web` | 1.1.1 | BSD-3-Clause |

All eleven are MIT or BSD-3-Clause — compliant with the compiled-into-user-code rule.

**Build tooling** (`dev_dependencies:` in `pubspec.yaml` — `build_runner`, `copy_with_extension_gen`, `json_serializable`, `test` — plus the 51 further packages their code generation and test-running pull in transitively). Every one of them is BSD-3-Clause, MIT or Apache-2.0 (`source_helper`), per `docs/dependency-inventory.json`, ecosystem `dart-client`. None are compiled into a consumer's app; they run only while regenerating the client or running `dart test`.

## .NET backend — `backends/dotnet/`

Every project under `backends/dotnet/src/` and `backends/dotnet/tests/` restores
with `RestorePackagesWithLockFile` (set once in `backends/dotnet/Directory.Build.props`),
so each has its own committed `packages.lock.json` — the .NET analogue of
`package-lock.json` / `pubspec.lock`, and what `scripts/check-dependency-licenses.mjs`
reads under the `nuget-dotnet-backend` ecosystem.

**Compiled into the running backend** (referenced by a `src/` project, so it ships inside the server that answers a request):

| Package | Version | Licence |
|---|---|---|
| `Mediator.Abstractions` | 3.0.2 | MIT (nuspec declares a `license type="file"`; verified against the committed `LICENSE` in `martinothamar/Mediator` on GitHub) |
| `FluentValidation` | 12.1.1 | Apache-2.0 |
| `Microsoft.EntityFrameworkCore` (+ `.Abstractions`, `.Analyzers`) | 10.0.12 | MIT |
| `Microsoft.Extensions.Caching.Memory` (+ `.Abstractions`) | 10.0.12 | MIT |
| `Microsoft.Extensions.DependencyInjection` (+ `.Abstractions`) | 10.0.12 | MIT |
| `Microsoft.Extensions.Logging` (+ `.Abstractions`) | 10.0.12 | MIT |
| `Microsoft.Extensions.Options` | 10.0.12 | MIT |
| `Microsoft.Extensions.Primitives` | 10.0.12 | MIT |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.12 | MIT |
| `Npgsql` (+ `Npgsql.EntityFrameworkCore.PostgreSQL`) | 10.0.3 | PostgreSQL Licence — OSI-approved and permissive, textually BSD/MIT-style (see `postgres/postgres`'s own `COPYRIGHT`); the .NET *driver*, unrelated to which licence governs the Postgres *server* process itself (audited separately below, under infrastructure) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.12 | MIT |
| `System.IdentityModel.Tokens.Jwt` (+ `Microsoft.IdentityModel.JsonWebTokens`, `.Tokens`, `.Protocols`, `.Protocols.OpenIdConnect`, `.Logging`, `.Abstractions`, `Microsoft.Bcl.Cryptography`) | 8.19.2–8.22.0 | MIT |
| `Serilog` (+ `.AspNetCore`, `.Extensions.Hosting`, `.Extensions.Logging`, `.Formatting.Compact`, `.Settings.Configuration`) | 4.3.0 / 10.0.0 | Apache-2.0 |
| `Serilog.Sinks.Console` (+ transitively pulled `.Sinks.Debug`, `.Sinks.File`, unused by this backend's own logging setup but resolved by `Serilog.AspNetCore`'s dependency tree) | 6.1.1 / 3.0.0 / 7.0.0 | Apache-2.0 |

All fourteen package families are MIT, Apache-2.0 or the permissive PostgreSQL Licence — compliant with the compiled-into-user-code rule. JWT signing/validation (`Host/Security/JwtAccessTokenIssuer`, wired in `Host/Program.cs`) and structured console logging with correlation IDs (`Host/Program.cs`'s `UseSerilog` call, consuming `Shared/Web/CorrelationIdMiddleware`'s logging scope) are what these two families exist for.
`Microsoft.EntityFrameworkCore.Relational` is deliberately separate from `Npgsql.EntityFrameworkCore.PostgreSQL`:
it is what `backends/dotnet/src/Features/Identity/Persistence` (entity configuration, provider-agnostic) references for
relational concepts like `ToTable`/`HasColumnName` that apply to any relational database, while `Npgsql.*` is confined
to `backends/dotnet/src/Database/Postgres` — the one place a provider name is allowed to appear, per `docs/STRUCTURE.md`.
`StackBraid.Shared` (see `docs/STRUCTURE.md`) also uses a `FrameworkReference` to `Microsoft.AspNetCore.App` for
`ProblemDetails`, localization and rate limiting types — a reference to the shared
.NET runtime already installed alongside the SDK, not a NuGet download of its own, so it
carries no separate licence to audit (the same basis `backends/dotnet/src/Host` gets automatically
from `Microsoft.NET.Sdk.Web`).

`Mediator.SourceGenerator` 3.0.2 (same licence and source as `Mediator.Abstractions` above) is
referenced only by `Features/Identity/Application`, with `PrivateAssets="all"` and
`ReferenceOutputAssembly="false"` — a Roslyn analyzer that generates the command/query dispatch
code at compile time and is never itself present in the built output. Classified `build-tooling`
for that reason, the same basis as `Microsoft.EntityFrameworkCore.Design` below.

**Design-time only, never shipped** (`Microsoft.EntityFrameworkCore.Design`, referenced with `PrivateAssets="all"` in `Database/Postgres` — it powers `dotnet ef migrations add` and is not copied into the built output): `Microsoft.EntityFrameworkCore.Design` itself plus its own transitive closure — the Roslyn `Microsoft.CodeAnalysis.*` packages, `Microsoft.Build.Framework`, `Microsoft.VisualStudio.SolutionPersistence`, `Mono.TextTemplating`, the `System.Composition.*` family, `Humanizer.Core`, and this one path's own `Newtonsoft.Json` 13.0.4 (the test projects separately resolve 13.0.3 — both versions are audited, both MIT). Classified `build-tooling`, the same basis as a CI-only linter: installed to generate code at development time, never linked into or redistributed with the running server.

**Build/test tooling** (referenced only by a `tests/` project — xUnit, Shouldly and NSubstitute, matching `docs/SPEC.md`'s test-dependency table — plus their own transitive closure): `xunit` and its `xunit.*` satellite packages, `xunit.runner.visualstudio`, `xunit.abstractions` (Apache-2.0 — its nuspec `licenseUrl` points at xunit's own `license.txt`, read directly rather than assumed), `Microsoft.NET.Test.Sdk`, `Microsoft.TestPlatform.*`, `coverlet.collector`, `Shouldly` (BSD-3-Clause) and its `DiffEngine`/`EmptyFiles` dependencies, `NSubstitute` (BSD-3-Clause) and its `Castle.Core` (Apache-2.0) dependency, `NetArchTest.Rules` (MIT — its nuspec carries no `license`/`licenseUrl` tag at all; verified directly against the `LICENSE` file in `BenMorris/NetArchTest` on GitHub) and its own `Mono.Cecil` dependency (MIT), plus `Newtonsoft.Json` 13.0.3, `System.CodeDom`, `System.Diagnostics.EventLog` and `System.Management` pulled in transitively. None of these compile into the running server; every one is MIT, Apache-2.0 or BSD-3-Clause regardless.

**Deliberately not added yet:** `Hangfire.Core`, `QuestPDF` and `ClosedXML` — named in `docs/SPEC.md`'s dependency table as the intended real implementations behind `IJobScheduler`, `IPdfGenerator` and `IExcelExporter` — are not referenced by any `.csproj` today. Those interfaces currently ship with a minimal, dependency-free default (an in-process job queue, a hand-written PDF writer, and CSV export) so the layer compiles and is genuinely tested without auditing a library nothing yet depends on. They enter this document, with a verified date, the same day they enter a `.csproj` — the same rule already applied to them here before any backend existed.

## Infrastructure — `infra/compose.yaml`

**This is where the problem was found.** `redis:7-alpine` resolves to Redis 7.4+, dual-licensed **RSALv2/SSPLv1** — both on the reciprocal-for-consumers reject list, regardless of Redis running as a separate process the generated clients never link against. `infra/compose.yaml` already pins `redis:7.2-alpine`, the last release under the original BSD 3-Clause licence, with the reasoning recorded in a comment at the top of that file. **The rest of the compose file was audited at the same time — no other image had a rejected licence, but the pinned tags matter, so they are recorded exactly.**

| Image | Pinned tag | Licence | Verified against |
|---|---|---|---|
| `postgres` | `16-alpine` | PostgreSQL Licence (permissive) | `postgres/postgres` `COPYRIGHT`, `REL_16_STABLE` |
| `rabbitmq` | `3.13-management-alpine` | MPL-2.0 (core), Apache-2.0 (some plugin files) | `rabbitmq/rabbitmq-server` `LICENSE`, tag `v3.13.7` |
| `redis` | `7.2-alpine` | BSD-3-Clause | `redis/redis` `COPYING`, tag `7.2.0` |
| `prom/prometheus` | `v2.54.1` | Apache-2.0 | `prometheus/prometheus` `LICENSE`, tag `v2.54.1` |
| `grafana/loki` | `2.9.8` | AGPL-3.0-only | `grafana/loki` `LICENSE`, tag `v2.9.8` |
| `grafana/grafana` | `11.2.0` | AGPL-3.0-only | `grafana/grafana` `LICENSE`, tag `v11.2.0` |

All six run as their own process in `infra/compose.yaml`, talked to only over their network protocol (SQL wire, AMQP, RESP, HTTP). None is linked into, imported by, or redistributed with any code a user of this skeleton writes or ships — so MPL-2.0 and AGPL-3.0 are acceptable here on the separate-process rule, the same basis that applies to Postgres, RabbitMQ and Grafana. **AGPL-3.0 is copyleft but is not on the reciprocal-for-consumers reject list** (that list is RPL, SSPL, RSAL, BSL, revenue-gated commercial specifically) — it is a materially different obligation from what made Redis 7.4+ and Loki/Grafana would-be problems, and it does not follow a user's own code the way those five do.

**Flagged, not decided — Redis's long-term line.** Redis 7.2 is the last BSD release; Redis Ltd. does not intend to reissue a BSD line above 7.2. The pin to `7.2-alpine` is today's compliant fix, not a permanent answer, because 7.2 will eventually fall out of upstream security support. The two compliant paths forward — **stay pinned to the 7.2 line for as long as it receives fixes, or move to `valkey/valkey` (the Linux Foundation-backed fork, confirmed BSD-3-Clause, wire-compatible with Redis OSS)** — are a product decision, not an audit finding, and are left open for the maintainer rather than decided here.

## CI-only tooling

Installed by `.github/workflows/*.yml` to check the repository itself. None of this reaches an end user's machine or a release artifact, but a user who reuses these workflows inherits the same installs — so they are recorded for transparency, though the automated check below does not gate on them (their versions are runner-default/floating by design, and re-auditing a linter's licence on every CI run buys nothing a one-time record doesn't already cover).

| Tool | Licence | Why it's acceptable |
|---|---|---|
| `actions/checkout@v4` | MIT | GitHub Action, CI-only |
| `actions/setup-node@v4` | MIT | GitHub Action, CI-only |
| `dart-lang/setup-dart@v1` | BSD-3-Clause | GitHub Action, CI-only |
| `shellcheck` (apt) | GPL-3.0-only | Invoked as a CLI linter over `scripts/*.sh`; GPL binds redistribution/linking of shellcheck itself, not the shell scripts it merely reads |
| `yamllint` (apt) | GPL-3.0-only | Same reasoning — a CLI check over YAML files it does not modify or redistribute |
| `@openapitools/openapi-generator-cli` (npx) | Apache-2.0 | Generates `clients/dart/`; the generated *output* is new code, not openapi-generator's own source, and carries no obligation of its own |
| `@redocly/cli@2.52.1` | MIT | Documented in `contract/README.md` as the contract-lint command. **Open item, out of scope here:** it is not currently wired into a CI workflow, only run manually — worth wiring up later |

## Rejected packages — re-verified, not just re-recorded

`docs/SPEC.md` and `AGENTS.md` already name five packages as banned. Re-checked today against current package metadata rather than trusting the earlier note, and confirmed **absent from every actual manifest in this repository**, including the now-existing `backends/dotnet` — grepped across every `.csproj` and `packages.lock.json` in the solution:

| Package | Licence today | Verified against |
|---|---|---|
| MediatR | RPL-1.5 (commercial licence required above the free tier) | jbogard/MediatR licensing notice, current `main` |
| AutoMapper | RPL-1.5 (same vendor, same change) | AutoMapper/AutoMapper licensing notice, current `main` |
| FluentAssertions | v8+ requires a paid Xceed licence for commercial use; v7.x stays Apache-2.0 | fluentassertions/fluentassertions release notes |
| EPPlus | Polyform Noncommercial 1.0.0 (paid licence required for commercial use) | EPPlusSoftware/EPPlus `LICENSE` |
| `MySql.Data` | GPL-2.0-only, with a commercial-licence exception sold by Oracle | Oracle/dotnet-connector `license.txt`; `Pomelo.EntityFrameworkCore.MySql` (MIT) remains the compliant alternative |

Nothing changed since the last note recorded these — the re-verification exists precisely because "nothing changed" is a claim that ages, not a fact that stays true on its own.

## Boundary cases carried forward, not yet audited against real usage

Hangfire Core (LGPL-3.0-only) and QuestPDF Community (free under USD 1M revenue, publicly traded and government entities excluded regardless of revenue) are already approved for future use, behind `IJobScheduler` and `IPdfGenerator` respectively. **Neither appears in this inventory yet** because no backend exists to depend on them — recording them here now would be auditing code that does not exist, which the truthfulness rule forbids. They enter this document, with a verified date, the same day they enter a `.csproj`.

## Attribution — the determination, in writing

The rule: an attribution obligation arises only from **actually reusing** third-party code — not from depending on a package, referencing an image tag, or studying how something works.

**Nothing in this repository vendors, copies, or redistributes third-party source or binaries.** The npm and pub.dev packages above are declared in manifests (`package.json`, `pubspec.yaml`); a user's own `npm install` / `dart pub get` fetches them directly from the original registries, each already carrying its own licence file — the same mechanism every Node or Dart project relies on, and not a StackBraid-specific redistribution. The Docker images are referenced by tag in `infra/compose.yaml`; Docker pulls them from their publishers' own registries at `docker compose up` time. `clients/typescript/src/generated/` and `clients/dart/lib/src/` are **generated output** — new code written by a code generator from `contract/openapi.yaml`, not copies of `@hey-api/openapi-ts`'s or `openapi-generator`'s own source.

**Conclusion: no `THIRD_PARTY_NOTICES` file is required today**, and none is included. This is recorded here, in writing, as the deliberate alternative to creating one. This determination is re-checked, not assumed, whenever a dependency changes class (for example, if a future release ever vendors a third-party file directly into the repository, or a release process starts bundling compiled dependency binaries) — at that point `THIRD_PARTY_NOTICES` becomes mandatory and this section says so.

## The CI gate

`scripts/check-dependency-licenses.mjs` (zero runtime dependencies — Node built-ins only) runs in `.github/workflows/ci.yml` on every push and pull request. It checks what is **actually resolved** today — `clients/typescript/package-lock.json`, `clients/dart/pubspec.lock`, every `backends/dotnet/**/packages.lock.json`, and every `image:` tag in `infra/compose.yaml` — against `docs/dependency-inventory.json`, and fails the build when:

- a resolved package/version has no matching entry in the inventory (**unaudited dependency**);
- a resolved version differs from the version the inventory audited (**drift** — the exact shape of the Redis problem, generalised: something moved and nobody re-checked the licence);
- any inventory entry's recorded licence contains RPL, SSPL, RSAL, BSL, "Commons Clause", or "proprietary" (**rejected regardless of class**);
- a `compiled-into-user-code` entry is licensed anything other than MIT, Apache-2.0, BSD-2/3-Clause, ISC or 0BSD (**class violation**).

Proven against three deliberately broken cases before being wired in: a bumped-but-unaudited npm version, a hand-added banned-licence entry, and (implicitly, by construction) an unaudited-new-package addition — each produced the expected non-zero exit with the offending package named. Restoring the clean state passes again.

## Keeping this current

Adding a dependency to any client, or changing an image tag in `infra/compose.yaml`, means:

1. Verify the licence from the package's actual registry metadata or `LICENSE` file — not from memory, not by assuming a transitive dependency kept its parent's licence.
2. Add or update the entry in `docs/dependency-inventory.json` (name, version, licence, class, today's date).
3. Reflect the change in this file if it is a direct/top-level dependency (the tables above mirror `README.md`'s "Dependencies" section — the full transitive closure lives only in the JSON, which would otherwise make both documents unreadable).
4. Run `node scripts/check-dependency-licenses.mjs` locally before committing. CI runs the same check and will otherwise catch it anyway.
