# Dependency licence audit

**Every dependency shipped here is inherited by every user of this skeleton.** A licence that binds the user is a defect, not a detail — see `AGENTS.md` → "Dependencies" for the rule this audit enforces.

Two classes, audited differently:

| Class | Rule |
|---|---|
| Compiled into user code | MIT, Apache-2.0 or BSD only |
| Run as a separate process, or used only as a build/CI tool via its CLI | Copyleft acceptable — it is never linked into or redistributed with the user's code |

Anything reciprocal-for-consumers — **RPL, SSPL, RSAL, BSL, or a revenue-gated commercial licence** — is rejected regardless of class or quality.

This document is the narrative record. The machine-readable source of truth is `docs/dependency-inventory.json`, checked on every push by `scripts/check-dependency-licenses.mjs` (see "The CI gate" below). **A licence recorded from memory is not an audit** — every entry below was verified against the actual package's registry metadata or licence file on the date given, not carried forward from an earlier audit's notes.

**Truthfulness note:** both backends' Identity feature are implemented and conformance-tested (see `docs/ROADMAP.md` step 3); the Next.js and Angular frontends' web and admin shells are both implemented and verified against both backends (see each one's own README). The Flutter mobile shell's Identity feature (register, sign in, profile, sign out) is implemented and verified end-to-end against both backends on the macOS desktop run target only — not on an iOS or Android simulator, see `mobile/flutter/README.md`. This inventory covers exactly what is genuinely shipped today. It will grow, stack by stack, as each is actually built. Nothing below pre-audits code that does not exist.

Last verified: **2026-09-15**.

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

## Flutter mobile — `mobile/flutter/`

The Identity shell — register, sign in, a session that survives an app
restart, profile, sign out — consuming `clients/dart` (above) via a
`path:` dependency, the same unchanged-generated-client convention the
frontends use for `clients/typescript`. `mobile/flutter/pubspec.lock` is
read by `scripts/check-dependency-licenses.mjs` under the
`dart-mobile-flutter` ecosystem (73 entries — every package resolved
transitively too, since a compiled-into-user-code classification follows
the whole dependency chain, not just the direct one).

**Compiled into the app** (reachable from a `dependencies:` entry —
`stackbraid_client`'s own runtime closure, `flutter_secure_storage`'s, and
the Flutter framework's own):

| Package | Licence | Why it's here |
|---|---|---|
| `dio`, `dio_web_adapter`, `copy_with_extension`, `json_annotation` | MIT / MIT / MIT / BSD-3-Clause | `stackbraid_client`'s own runtime dependencies — see the Dart client section above |
| `firebase_core`, `firebase_messaging` (+ their platform-interface and web implementations, and `_flutterfire_internals`) | BSD-3-Clause | Firebase client initialization, device-token registration/refresh, and foreground/background/terminated message handling |
| `flutter_secure_storage` (+ its `_linux`/`_macos`/`_platform_interface`/`_web`/`_windows` platform packages) | BSD-3-Clause | Platform secure storage for the persisted refresh token — see `mobile/flutter/README.md`, "Token storage" |
| `path_provider` (+ its own platform packages), `jni`, `jni_flutter`, `jni_util`, `objective_c`, `ffi`, `win32`, `xdg_directories` | BSD-3-Clause | `flutter_secure_storage_windows`'s own dependency chain (path_provider) and, transitively, `path_provider_android`/`path_provider_foundation`'s own native-interop packages |
| `code_assets`, `hooks`, `crypto`, `logging`, `pub_semver`, `record_use`, `yaml`, `package_config`, `args` | BSD-3-Clause | `objective_c`/`jni`'s own further dependency chain |
| `characters`, `collection`, `material_color_utilities`, `meta`, `vector_math` | BSD-3-Clause / Apache-2.0 | Runtime dependencies of the Flutter framework itself |
| `intl` | BSD-3-Clause | Runtime dependency of `flutter_localizations` (bundled with the Flutter SDK, no licence entry of its own) — backs the Material/Widgets/Cupertino localization delegates this app registers for Spanish |
| `path`, `http_parser`, `source_span`, `string_scanner`, `term_glyph`, `typed_data`, `web` | BSD-3-Clause | Transitive runtime dependencies of `dio`/`http_parser` |

All compliant with the compiled-into-user-code rule — MIT, Apache-2.0 or
BSD-3-Clause throughout.

**Build/test tooling** (`dev_dependencies:` and the transitive closure of
`flutter_test`/`integration_test`/`flutter_lints` — never compiled into the
shipped app): `flutter_lints`, `lints`, `matcher`, `test_api`,
`boolean_selector`, `stream_channel`, `stack_trace`, `clock`, `fake_async`,
`leak_tracker` (+ `_flutter_testing`, `_testing`), `vm_service`, `webdriver`,
`sync_http`, `file`, `process` — all MIT, Apache-2.0 or BSD-3-Clause per
`docs/dependency-inventory.json`.

`flutter`, `flutter_test`, `flutter_localizations` and `integration_test`
themselves are `source: sdk` in `pubspec.lock` — bundled with the Flutter
SDK, not fetched from pub.dev, and carry no licence entry of their own,
the same basis `Microsoft.AspNetCore.App`'s `FrameworkReference` is
exempted on below. `stackbraid_client` is `source: path` — this
repository's own package, not a third-party dependency, the same workspace
exception `@stackbraid/client-typescript` gets in the npm ecosystems.

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
| `Serilog` (+ `.AspNetCore`, `.Extensions.Hosting`, `.Extensions.Logging`, `.Formatting.Compact`, `.Settings.Configuration`) | 4.4.0 / 10.0.0 | Apache-2.0 |
| `Serilog.Sinks.Console` (+ transitively pulled `.Sinks.Debug`, `.Sinks.File`, unused by this backend's own logging setup but resolved by `Serilog.AspNetCore`'s dependency tree) | 6.1.1 / 3.0.0 / 7.0.0 | Apache-2.0 |
| `OpenTelemetry` (+ `.Api`, `.Api.ProviderBuilderExtensions`, `.Extensions.Hosting`) | 1.18.0 | Apache-2.0 |
| `OpenTelemetry.Instrumentation.AspNetCore` (+ `.Instrumentation.Http`) | 1.18.0 | Apache-2.0 |
| `OpenTelemetry.Exporter.Console` | 1.18.0 | Apache-2.0 |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` (registered unconditionally in code, but only actually added to the tracing/metrics pipeline — see `Host/Observability/OpenTelemetryExtensions.cs` — when `Otel:OtlpEndpoint` is configured; this process never dials a collector nobody asked it to) | 1.18.0 | Apache-2.0 |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` (+ `StackExchange.Redis`, `Pipelines.Sockets.Unofficial`, `MessagePack`, `MessagePack.Annotations`, `Microsoft.NET.StringTools`) | 10.0.12 / 2.7.27 / 2.2.8 / 2.5.302 / 17.6.3 | MIT |
| `AWSSDK.S3` (+ `AWSSDK.Core`) | 3.7.415.5 / 3.7.402.6 | Apache-2.0 |
| `ClosedXML` (+ `ClosedXML.Parser`, `DocumentFormat.OpenXml`, `DocumentFormat.OpenXml.Framework`, `ExcelNumberFormat`, `RBush`, `SixLabors.Fonts`, `System.IO.Packaging`) | 0.104.2 / 1.2.0 / 3.1.1 / 1.1.0 / 4.0.0 / 1.0.0 / 8.0.1 | MIT / Apache-2.0 |
| `QuestPDF` | 2024.12.3 | QuestPDF Community (free under USD 1M annual revenue and for OSI-approved open-source projects under category 5 of License Selection Guide; swappable behind `IPdfGenerator`) |

Every package family in the table above is MIT, Apache-2.0 or the permissive PostgreSQL Licence — compliant with the compiled-into-user-code rule. `Microsoft.AspNetCore.SignalR.StackExchangeRedis` is the realtime layer's optional backplane (`Host/Program.cs`, `Realtime:BackplaneConnectionString`) — layered underneath SignalR only when a connection string is configured, so one instance needs nothing running at all. It talks the plain Redis wire protocol, which Valkey speaks unchanged, so the same code runs against either without a rebuild; `docs/SPEC.md` §13's realtime scope names the choice between them as still open. `MessagePack` arrived transitively for server-to-server invocation serialization inside the backplane itself — unrelated to this project's own client-facing JSON hub protocol (`Host/Program.cs` pins `AddJsonProtocol`) — and is pinned via the direct `Microsoft.AspNetCore.SignalR.StackExchangeRedis` reference at 10.0.12 specifically: resolving that package at its initial 10.0.0 release pulls `MessagePack` 2.5.187, which NuGet's own vulnerability audit (`dotnet restore`'s `NU1902`/`NU1903` warnings) flags with several known CVEs; 10.0.12 resolves a patched 2.5.302 with none, verified by a clean `dotnet restore` with no such warnings. JWT signing/validation (`Host/Security/JwtAccessTokenIssuer`, wired in `Host/Program.cs`), structured console logging with correlation IDs (`Host/Program.cs`'s `UseSerilog` call, consuming `Shared/Web/CorrelationIdMiddleware`'s logging scope), and traces/metrics instrumentation (`Host/Observability/OpenTelemetryExtensions.cs`, also consuming the same correlation ID as a span attribute) are what these three families exist for.
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

**Build/test tooling** (referenced only by a `tests/` project — xUnit, Shouldly and NSubstitute, matching `docs/SPEC.md`'s test-dependency table — plus their own transitive closure): `xunit` and its `xunit.*` satellite packages, `xunit.runner.visualstudio`, `xunit.abstractions` (Apache-2.0 — its nuspec `licenseUrl` points at xunit's own `license.txt`, read directly rather than assumed), `Microsoft.NET.Test.Sdk`, `Microsoft.TestPlatform.*`, `coverlet.collector`, `Shouldly` (BSD-3-Clause) and its `DiffEngine`/`EmptyFiles` dependencies, `NSubstitute` (BSD-3-Clause) and its `Castle.Core` (Apache-2.0) dependency, `NetArchTest.Rules` (MIT — its nuspec carries no `license`/`licenseUrl` tag at all; verified directly against the `LICENSE` file in `BenMorris/NetArchTest` on GitHub) and its own `Mono.Cecil` dependency (MIT), `Microsoft.AspNetCore.Mvc.Testing` (+ `Microsoft.AspNetCore.TestHost`, MIT) — the in-process `WebApplicationFactory<Program>` the secret-redaction test drives the real Identity flow through — plus `Newtonsoft.Json` 13.0.3, `System.CodeDom`, `System.Diagnostics.EventLog` and `System.Management` pulled in transitively. None of these compile into the running server; every one is MIT, Apache-2.0 or BSD-3-Clause regardless.

**Deliberately not added yet:** `Hangfire.Core` — named in `docs/SPEC.md`'s dependency table as the intended durable implementation behind `IJobScheduler` — is not referenced by any `.csproj` today. `IJobScheduler` currently ships with a minimal, dependency-free in-process background queue (`InProcessJobScheduler`). `ClosedXML` (0.104.2, MIT), `QuestPDF` (2024.12.3, QuestPDF Community), and `AWSSDK.S3` (3.7.415.5, Apache-2.0) are now referenced by `backends/dotnet/src/Shared` and audited as of 2026-09-24.

## Python backend — `backends/python/`

`backends/python/requirements-lock.txt` (`uv pip freeze` output, committed) is
this backend's analogue of `package-lock.json` / `pubspec.lock` /
`packages.lock.json` — the exact resolved graph for both the runtime and
dev/test dependency groups, read by `scripts/check-dependency-licenses.mjs`
under the `pypi-python-backend` ecosystem.

**Compiled into the running backend:**

| Package | Version | Licence |
|---|---|---|
| `fastapi` | 0.141.1 | MIT |
| `uvicorn` (+ `uvloop`, `httptools`, `watchfiles`, `websockets` — the `[standard]` extra) | 0.52.4 / 0.22.1 / 0.8.0 / 1.2.0 / 17.1 | MIT / MIT / MIT / MIT / BSD-3-Clause |
| `pydantic` (+ `pydantic-core`, `annotated-types`, `typing-inspection`) | 2.13.5 | MIT |
| `pydantic-settings` (+ `python-dotenv`) | 2.15.0 / 1.2.3 | MIT / BSD-3-Clause |
| `sqlalchemy` (+ `greenlet`) | 2.0.52 / 3.5.5 | MIT / MIT AND PSF-2.0 |
| `asyncpg` | 0.31.0 | Apache-2.0 |
| `alembic` (+ `mako`) | 1.20.0 / 1.4.1 | MIT |
| `pyjwt` | 2.14.0 | MIT |
| `starlette`, `anyio`, `click`, `idna`, `markupsafe`, `pyyaml`, `typing-extensions`, `annotated-doc` | various | MIT / BSD-3-Clause / PSF-2.0 |
| `opentelemetry-api` (+ `.sdk`, `.instrumentation`, `.instrumentation-asgi`, `.instrumentation-fastapi`, `.semantic-conventions`, `.util-http`) | 1.44.0 / 0.65b0 | Apache-2.0 |
| `opentelemetry-exporter-otlp-proto-http` (+ `.exporter-otlp-proto-common`, `.proto`) — registered in code but only actually added to the tracing/metrics pipeline when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured; this process never dials a collector nobody asked it to | 1.44.0 | Apache-2.0 |
| `protobuf`, `googleapis-common-protos` (transitive, OTLP wire format) | 7.36.1 / 1.75.3 | BSD-3-Clause / Apache-2.0 |
| `requests`, `urllib3`, `charset-normalizer` (transitive, the HTTP transport `opentelemetry-exporter-otlp-proto-http` actually sends over) | 2.34.2 / 2.7.0 / 3.5.1 | Apache-2.0 / MIT / MIT |
| `asgiref`, `wrapt` (transitive, instrumentation plumbing) | 3.12.1 / 2.4.1 | BSD-3-Clause / BSD-2-Clause |
| `redis` | 5.3.1 | MIT |

`redis` (`redis.asyncio`, `app/shared/realtime/publisher.py`) is the realtime
layer's optional backplane, the Python counterpart of the .NET table's
`Microsoft.AspNetCore.SignalR.StackExchangeRedis` row above — used only when
`STACKBRAID_REALTIME_REDIS_URL` is configured (`app/host/config.py`); left
unset, `InProcessRealtimePublisher` delivers to this process's own WebSocket
connections with no package call at all. It speaks the plain Redis wire
protocol, which Valkey speaks unchanged, so `docs/SPEC.md` §13's still-open
choice between the two is not decided by this dependency. Pulled in with no
new transitive package of its own — its only declared dependency, `pyjwt`,
is already audited above.

Every one is MIT, Apache-2.0, BSD-2/3-Clause or PSF-2.0 — compliant with the
compiled-into-user-code rule. Notably absent by deliberate choice: **no
mediator library** — `application/` resolves command/query handlers directly
from FastAPI's own dependency graph (see `AGENTS.md`'s "the only intended
difference between the backends") — and **no ORM-adjacent Postgres driver
beyond `asyncpg`**: `psycopg` was considered and rejected in favour of
`asyncpg`'s clean Apache-2.0 licence over psycopg's LGPL-shaded one, since
Apache-2.0 needs no further reasoning under the compiled-into-user-code rule.
JWT signing/validation (`features/identity/endpoints/security.py`),
structured request correlation (`shared/web/correlation.py`), and
traces/metrics instrumentation (`shared/observability/tracing.py`, also
consuming the same correlation ID as a span attribute) are what `pyjwt`,
`starlette`'s middleware hooks, and the `opentelemetry-*` family exist for,
respectively.

**Build/test tooling** (installed only via the `dev` extra — `pytest`,
`pytest-asyncio`, `httpx`, `import-linter` — plus their own transitive
closure: `grimp`, `iniconfig`, `pluggy`, `pygments`, `markdown-it-py`,
`mdurl`, `rich`, `certifi`, `httpcore`, `h11`, `packaging`): every one is
MIT, Apache-2.0, BSD-2/3-Clause or MPL-2.0 (`certifi` — a CA bundle, only
ever read by `httpx`'s test client, never linked into or shipped with the
running server). None compile into the running backend.

**Deliberately not added yet:** a background-job runner and a real PDF/Excel
library — named in `docs/SPEC.md`'s dependency table as the intended real
implementations behind `JobScheduler` and `PdfGenerator`/`ExcelExporter`.
Those interfaces currently ship with a minimal, dependency-free default (a
hand-written PDF writer, RFC 4180 CSV) — the same pattern the .NET backend's
`Shared` layer already established. They enter this document, with a
verified date, the same day they enter `pyproject.toml`. `InProcessJobScheduler`
(`app/shared/jobs/scheduler.py`) is wired for real in `host/main.py`'s
lifespan for the first time this session — the realtime layer's demo job
needed a real queue to run on — and still needs no package of its own; only
the realtime publisher's distributed mode reaches for `redis`.

## Next.js frontend — `frontends/nextjs/`

`frontends/nextjs/package-lock.json` is the exact resolved graph, read by
`scripts/check-dependency-licenses.mjs` under the `npm-nextjs-frontend`
ecosystem (915 entries in `docs/dependency-inventory.json` — by far the
largest single ecosystem here, because a full framework's own transitive
tree dwarfs a single generated API client's). `@stackbraid/client-typescript`
itself (linked in via `file:../../clients/typescript`, this repository's own
generated client — see above) is excluded from the count: it is a workspace
path, not a third-party package, and carries no licence entry of its own.

**Compiled into the shipped app** (top-level `dependencies` in `package.json`):

| Package | Licence |
|---|---|
| `next`, `react`, `react-dom` | MIT |
| `@tanstack/react-query` | MIT |
| `next-intl` | MIT |
| `radix-ui` (shadcn/ui's underlying primitives), `class-variance-authority`, `cn` (shadcn/ui's own class-merge helper, not the general-purpose npm package of the same name from a different author — verified against `shadcn-ui/cn`'s own repository), `lucide-react`, `next-themes`, `sonner`, `tw-animate-css` | MIT |

All top-level runtime dependencies are MIT. The full transitive closure —
including React's own dependency-free runtime, Radix UI's per-primitive
packages, and Tailwind's CSS engine pulled in through the PostCSS plugin —
is MIT, Apache-2.0, BSD-2/3-Clause or ISC, with one documented exception:

**Flagged for Anik, not decided — `sharp` and its bundled `@img/sharp-libvips-*` binaries.**
`sharp` is not something this project asked for: it is an *optional*
dependency `next` itself declares (image optimization for `next/image`),
and npm installs a matching platform build by default. This app never
calls `next/image` anywhere in `src/`, so it is present but genuinely
unused. `sharp` itself is Apache-2.0; the native `libvips` binary it bundles
per-platform (`@img/sharp-libvips-*`) is **LGPL-3.0-or-later** — not on the
rejected-regardless-of-class list (RPL/SSPL/RSAL/BSL/Commons-Clause/
revenue-gated), but also not MIT/Apache/BSD, so it does not clear the
compiled-into-user-code bar by the letter of the rule either. It is recorded
in the inventory as `separate-process` (a native addon invoked in-process
through Node's N-API, never statically linked into the JavaScript bundle
served to a browser or into the Node server bundle itself) rather than
silently passed through as compliant — the same honest treatment the Redis
Valkey question already received below. Removing it outright risks taking
`@next/swc-*`/`@parcel/watcher-*` down with it (also `optionalDependencies`,
but genuinely required for the build on any given platform, unlike `sharp`);
the safer fix, if one is wanted, is a scoped `overrides` entry or dropping
`next/image` support from the skeleton's own documentation. Anik's call.

**Build/test tooling** (`devDependencies` — never bundled into anything shipped
to a browser or a deployed server): `typescript`, `tailwindcss` (+ `@tailwindcss/postcss`),
`eslint` (+ `eslint-config-next`), `shadcn` (the component-registry CLI, used only to
add components at development time), `vitest` (+ `@vitejs/plugin-react`, `jsdom`),
`@testing-library/react` (+ `@testing-library/dom`), `@playwright/test`,
`dependency-cruiser`, and `@types/*`. Two MPL-2.0 packages appear here
(`lightningcss`, Tailwind v4's CSS parser, and `axe-core`, pulled in
transitively by the Playwright/testing toolchain) — both run only at build
or test time, never linked into or redistributed with the deployed app,
which is exactly the basis `build-tooling` already applies to elsewhere in
this document (RabbitMQ's MPL-2.0 licence, audited below under
infrastructure, is the same licence family accepted there for the same
reason: it never reaches a consumer's own code).

## Angular frontend — `frontends/angular/`

`frontends/angular/package-lock.json` is the exact resolved graph, read by
`scripts/check-dependency-licenses.mjs` under the `npm-angular-frontend`
ecosystem (805 entries in `docs/dependency-inventory.json`). Same workspace
exception as the Next.js frontend above: `@stackbraid/client-typescript`
(`file:../../clients/typescript`) carries no licence entry of its own.

**Compiled into the shipped app** (top-level `dependencies` in `package.json`):

| Package | Licence |
|---|---|
| `@angular/core`, `@angular/common`, `@angular/compiler`, `@angular/forms`, `@angular/platform-browser`, `@angular/router`, `@angular/animations` | MIT |
| `@angular/material`, `@angular/cdk` | MIT |
| `@jsverse/transloco` | MIT |
| `@tanstack/angular-query-experimental` | MIT |
| `rxjs`, `tslib` | Apache-2.0, 0BSD |

Every one of the 38 packages resolved into the compiled-into-user-code class
is MIT, Apache-2.0, BSD-2-Clause, ISC, 0BSD or Python-2.0 — all permissive,
no exceptions and nothing flagged. Angular Material was chosen specifically
because it needs no separate licence review the way some commercial Angular
UI kits do (checked against the actual `LICENSE`/`license` field in each
candidate's own package metadata before adding it, per the rule in
`AGENTS.md` §7 — not carried forward from general reputation).

**Build/test tooling** (`devDependencies`): `@angular/cli`, `@angular/build`,
`@angular/compiler-cli`, `typescript`, `tailwindcss` (+ `postcss`,
`autoprefixer`), `eslint` (+ `angular-eslint`, `typescript-eslint`),
`@playwright/test`, `dependency-cruiser`, `jsdom`, `vitest`. All MIT,
Apache-2.0, BSD or ISC; none are bundled into anything shipped to a browser.

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

## Vendored browser assets — `contract/docs-assets/`

**This is the one place the repository redistributes someone else's code.** Everything else here is
declared in a manifest and fetched by the user's own tooling; these files are copied into the tree
and ship with every clone.

| Package | Version | Licence | Class | Verified against | Verified on |
|---|---|---|---|---|---|
| `swagger-ui-dist` | 5.32.15 | Apache-2.0 | Compiled into user code | the package's own `LICENSE` file, copied to `contract/docs-assets/swagger-ui/LICENSE`, cross-checked against npm registry metadata | 2026-09-15 |

Only `swagger-ui.css` and `swagger-ui-bundle.js` are vendored; both backends serve them at
`/docs/assets/`. Audited on the strict **compiled-into-user-code** rule, not the separate-process
one: this code is shipped to and executed in the browser of every person who opens the API
documentation. Apache-2.0 satisfies that rule, and its attribution requirement is met by
`THIRD_PARTY_NOTICES.md` and the licence files copied alongside the code.

`swagger-ui-dist` declares one dependency of its own, `@scarf/scarf`, which reports installation
telemetry. It is **not vendored and not installed** — it is an npm install-time package, absent from
the browser bundles, which were checked for it directly. Nothing in `swagger-ui-bundle.js` contacts
a third party once Swagger UI's own validator badge is disabled, which the documentation page does
(`validatorUrl: null`) and both backends' tests assert.

**Why this is a copy rather than a CDN reference** — so the documentation renders on an air-gapped
network, so no third party is told who reads it, and so the bytes the browser executes are the bytes
audited here. The full reasoning, and the cost accepted in exchange, is in
`contract/docs-assets/swagger-ui/README.md`.

**The integrity record.** `contract/docs-assets/swagger-ui/PROVENANCE.json` carries the package,
version, licence, the npm tarball integrity hash, and a SHA-256 per file. The CI gate below re-hashes
every one on each run: a vendored file cannot be edited, and its version cannot be bumped, without
the build failing until the audit is redone.

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
| `@redocly/cli@2.52.1` | MIT | Documented in `contract/README.md` as the contract-lint command; wired into CI (`.github/workflows/ci.yml` lint job) to ensure the served contract is validated on every push |

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

## Boundary cases audited against real usage

QuestPDF Community (free under USD 1M annual revenue, and for open-source projects distributed under an OSI-approved license under category 5 of its License Selection Guide; publicly traded and government entities excluded regardless of revenue) is now audited and active behind `IPdfGenerator` as of 2026-09-24. StackBraid is an MIT-licensed open-source project and qualifies under both the open-source and revenue criteria. The `IPdfGenerator` abstraction guarantees that callers outside QuestPDF's Community tier can swap in `MinimalPdfGenerator` without code changes.

Hangfire Core (LGPL-3.0-only) remains approved for future durable job scheduling behind `IJobScheduler`, but is not yet referenced in any `.csproj`.

## Attribution — the determination, in writing

The rule: an attribution obligation arises only from **actually reusing** third-party code — not from
depending on a package, referencing an image tag, or studying how something works.

**This repository now redistributes third-party code, in exactly one place.**
`contract/docs-assets/swagger-ui/` holds two files copied byte-identical from the published
`swagger-ui-dist` package and served to browsers by both backends. That is redistribution in the
plain sense: the files travel with every clone, release and deployment of this repository. Apache-2.0
§4 requires the licence and notice to travel with them, so they do —
`contract/docs-assets/swagger-ui/LICENSE`, `NOTICE`, and the bundled-code notices in
`swagger-ui-bundle.js.LICENSE.txt` — and [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) at the
repository root names the package, version, licence and copyright holder.

**Everything else still creates no obligation, for the reason it never did.** The npm and pub.dev
packages above are declared in manifests (`package.json`, `pubspec.yaml`); a user's own
`npm install` / `dart pub get` fetches them directly from the original registries, each already
carrying its own licence file — the same mechanism every Node or Dart project relies on, and not a
StackBraid-specific redistribution. The Docker images are referenced by tag in `infra/compose.yaml`;
Docker pulls them from their publishers' own registries at `docker compose up` time.
`clients/typescript/src/generated/` and `clients/dart/lib/src/` are **generated output** — new code
written by a code generator from `contract/openapi.yaml`, not copies of `@hey-api/openapi-ts`'s or
`openapi-generator`'s own source.

**An earlier version of this document concluded that no `THIRD_PARTY_NOTICES` file was required, and
said so in writing.** That conclusion was correct when written and is recorded here rather than
quietly deleted, because it also named the condition that would end it: *"if a future release ever
vendors a third-party file directly into the repository … at that point `THIRD_PARTY_NOTICES` becomes
mandatory."* Vendoring Swagger UI is that event. The file was created in the same commit that copied
the first byte.

## The CI gate

`scripts/check-dependency-licenses.mjs` (zero runtime dependencies — Node built-ins only) runs in `.github/workflows/ci.yml` on every push and pull request. It checks what is **actually resolved** today — `clients/typescript/package-lock.json`, `clients/dart/pubspec.lock`, every `backends/dotnet/**/packages.lock.json`, `backends/python/requirements-lock.txt`, and every `image:` tag in `infra/compose.yaml` — against `docs/dependency-inventory.json`, and fails the build when:

- a resolved package/version has no matching entry in the inventory (**unaudited dependency**);
- a resolved version differs from the version the inventory audited (**drift** — the exact shape of the Redis problem, generalised: something moved and nobody re-checked the licence);
- any inventory entry's recorded licence contains RPL, SSPL, RSAL, BSL, "Commons Clause", or "proprietary" (**rejected regardless of class**);
- a `compiled-into-user-code` entry is licensed anything other than MIT, Apache-2.0, BSD-2/3-Clause, ISC or 0BSD (**class violation**);
- a vendored file under `contract/docs-assets/` no longer matches the SHA-256 recorded in its `PROVENANCE.json` (**vendored asset modified**), or that record names a version the inventory never audited (**unaudited upgrade**). Vendored copies have no lockfile to read, so without this check a bumped version or a hand-edited byte would be the one kind of change nothing here would notice.

Proven against five deliberately broken cases before being wired in: a bumped-but-unaudited npm version, a hand-added banned-licence entry, a bumped-but-unaudited `requirements-lock.txt` version (`fastapi` hand-edited to a version the inventory never audited), (implicitly, by construction) an unaudited-new-package addition, and a vendored Swagger UI file with five bytes appended — each produced the expected non-zero exit with the offending package or file named. Restoring the clean state passes again.

## Dependabot — what is on, what is off, and why

Two settings, often confused for one:

| Setting | State | Reason |
|---|---|---|
| Security alerts | **on** | Reports a real advisory against the default branch. This is what surfaced the `js-yaml` and `pytest` advisories. |
| Security updates (automatic fix pull requests) | **on** | Fires only when an advisory exists — a handful of times a year. It opens the fix we would otherwise have to find and write by hand. |
| Version updates for npm, pip, NuGet, pub | **off** | See below. |
| Version updates for GitHub Actions | **on**, monthly, grouped | `.github/dependabot.yml`. |

**Why routine version updates are off for the package ecosystems.** The CI gate above checks every *resolved* version against `docs/dependency-inventory.json`. A version-update pull request bumps a lock file, so it arrives failing — `VERSION DRIFT: <package> resolved at X, but the inventory only audited Y` — and stays failing until somebody re-verifies that package's licence at the new version and writes an inventory entry. Verified by simulating one: bumping a single lock-file entry by a patch release produced exactly that failure.

That is the audit doing its job, not an obstacle to route around. But it means each such pull request is manual audit work rather than a free upgrade, and across five ecosystems that is a standing tax on software that has not been released. A security advisory is worth paying it for. "A newer patch exists" is not.

**Why GitHub Actions are the exception.** They are recorded in the CI-only table above, and the automated check deliberately does not gate on them, so these updates land green. They are also the dependencies most likely to rot silently — a pinned action keeps working right up until the runner drops the Node version it targets.

**Dependabot's own commits carry no agent trailer**, because Dependabot is not one of our agents. The trailer goes on the merge commit when one of us reviews and merges its pull request.

## Keeping this current

Adding a dependency to any client, or changing an image tag in `infra/compose.yaml`, means:

1. Verify the licence from the package's actual registry metadata or `LICENSE` file — not from memory, not by assuming a transitive dependency kept its parent's licence.
2. Add or update the entry in `docs/dependency-inventory.json` (name, version, licence, class, today's date).
3. Reflect the change in this file if it is a direct/top-level dependency (the tables above mirror `README.md`'s "Dependencies" section — the full transitive closure lives only in the JSON, which would otherwise make both documents unreadable).
4. Run `node scripts/check-dependency-licenses.mjs` locally before committing. CI runs the same check and will otherwise catch it anyway.

**Vendoring or upgrading a copied file** additionally means updating its `PROVENANCE.json` (version,
source, tarball integrity, every file hash, today's date), updating `THIRD_PARTY_NOTICES.md`, and
re-reading the licence from the copied `LICENSE` file rather than carrying the old record forward.
`contract/docs-assets/swagger-ui/README.md` has the exact commands.
