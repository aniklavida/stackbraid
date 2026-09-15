# backends/dotnet

**Status: the first working backend.** Register, log in, refresh, log out,
manage users and roles — all implemented, all authenticated with real JWTs,
all backed by a real Postgres database, and **all proven green against
`contract/conformance`, the suite that is the single source of truth for
whether a backend actually satisfies the contract** (see "Conformance" below
for the real run). `docs/STRUCTURE.md`'s dependency rules are enforced, not
just followed by convention — `tests/ArchitectureTests` fails the build the
moment one is violated (see "Architecture tests" below). This is
`docs/ROADMAP.md` step 2's target. What is not yet true: no second provider
(SQL Server/MySQL), no second backend (Python), and none of the Tier 2
"professional" capabilities beyond the minimal defaults `Shared` already
ships (see below). Frontends and mobile do not exist yet.

```
backends/dotnet/
├── StackBraid.sln
├── Directory.Build.props    shared build settings (net10.0, nullable, per-project lock files)
├── scripts/                 start/stop a throwaway local Postgres for integration tests
├── src/
│   ├── Shared/               cross-cutting plumbing — see below
│   ├── Database/Postgres/    Npgsql wiring, migrations, seed data — see below
│   ├── Features/Identity/
│   │   ├── Domain/            entities, value objects, domain events — depends on nothing
│   │   ├── Persistence/       DbContext, entity configuration, repositories — provider-agnostic
│   │   ├── Contracts/         DTOs and requests mirroring contract/openapi.yaml
│   │   ├── Application/       every Command and Query the contract needs
│   │   └── Endpoints/         the HTTP surface — minimal API, permission-gated
│   └── Host/                 composition root — JWT, Serilog, health checks, DI wiring
└── tests/
    ├── Shared.UnitTests/                 real tests, all passing
    ├── Features.Identity.UnitTests/      domain + application + mapping, all passing
    ├── Features.Identity.IntegrationTests/   real local Postgres, all passing — see its own README
    └── ArchitectureTests/                 the layering rules, enforced — see below
```

## Conformance — the real evidence

```
$ CONFORMANCE_ADMIN_EMAIL="admin@stackbraid.local" CONFORMANCE_ADMIN_PASSWORD="<seeded>" \
    node cli/run.mjs http://127.0.0.1:<port>

39 passed, 0 failed, 1 skipped, 40 total.
```

Run against a genuinely fresh, empty local Postgres cluster (no Docker, no
Testcontainers — see `scripts/start-local-postgres.sh`) migrated and seeded
by this backend's own startup path. Every check in `contract/conformance`
passes: the byte-identical served contract at `/openapi.yaml`, browsable API
documentation at `/docs` served from a vendored copy of Swagger UI rather than
a CDN, absence of code-derived documentation endpoints,
schema shape, the RFC 9457 Problem envelope on every documented
error, offset pagination arithmetic, `UtcDateTime`'s exact `Z`-suffixed
format, both token-delivery paths (body and httpOnly cookie) for
login/refresh/logout, refresh-token rotation and revocation, permission-gated
admin endpoints, and the realtime payloads below. The one skip is the
expiry check, which needs a short-lived access token TTL
(`Jwt:AccessTokenLifetimeSeconds`) configured to exercise for real rather
than wait out the default 900 seconds.
The generated TypeScript and Dart clients (`clients/typescript`,
`clients/dart`) both call this backend successfully with no hand edits —
register, log in, and read the caller's own account, exercised directly
against a running instance.

Three real bugs an earlier run caught and fixed, worth recording because
this is exactly what the suite is for: the rate limit on auth endpoints was
tight enough that the suite's own traffic tripped it; ASP.NET Core's default
JWT challenge/forbid responses bypass the Problem-envelope pipeline entirely
(bare 401/403, no body); and `HttpResponse.WriteAsJsonAsync`'s
no-content-type overload silently stamps `application/json` over a
content type already set, which broke the envelope's content type on every
response written outside the normal `Results.Problem(...)` path.

## Realtime

`Host/Realtime/NotificationsHub.cs` (`/v1/hubs/notifications`) and
`Host/Realtime/JobsHub.cs` (`/v1/hubs/jobs`) push the `RealtimeMessage`
shapes `contract/openapi.yaml` defines. A client authenticates with
`?access_token=` on the connection (a browser WebSocket handshake carries no
custom headers) and is placed in group `user:{userId}` on connect for
notifications, or joins `job:{jobId}` itself for the jobs channel.
`Shared/Realtime/IRealtimePublisher` is the port the existing
deactivate/assign-role/revoke-role command handlers call — no new feature,
no new REST endpoint. `Host/Realtime/SignalRRealtimePublisher` is the only
implementation and never names a backplane technology itself.

A Redis (or Valkey — the client library speaks the plain wire protocol,
so either works unchanged) backplane is layered underneath only when
`Realtime:BackplaneConnectionString` is configured; left unset, delivery is
still correct for a single instance. **Proven for real**, both here and in
`contract/conformance`'s `checks/realtime.mjs`: two instances of this
backend behind the same Valkey process, a client connected to instance A
receiving a `user.role_changed`, `user.deactivated` and `job.progress`
raised through a REST call or a job start on instance B —
`scripts/verify-realtime-fanout.mjs` at the repository root reproduces this
against any two running instances.

## What `Shared` does

- **Persistence** — `AppDbContextBase`, a provider-agnostic `DbContext` base
  (a template-method save pipeline only — it knows nothing about any
  feature's entity types, so `Domain` genuinely depends on nothing) and
  `IRepository`/`RepositoryBase`/`IUnitOfWork` on top of it. No
  provider-specific type appears here — that is `Database/Postgres`'s job.
- **Web** — the RFC 9457 Problem envelope the contract requires
  (`ProblemDetailsMapper`, `GlobalExceptionHandler`, `ResultHttpExtensions`),
  a correlation ID middleware, and the one rate-limit policy every backend
  needs on day one (`RateLimitingExtensions`).
- **Localization** — `IAppLocalizer`, backed by two embedded JSON catalogues
  (English and Spanish) — genuinely resolves per `Accept-Language`, not a
  stub.
- **Security** — `IPasswordHasher` (PBKDF2-HMAC-SHA256, OWASP's current
  minimum) and `OpaqueTokenGenerator` (a fast SHA-256 hash for refresh
  tokens — deliberately not PBKDF2, which would punish the read-heavy
  per-request lookup a slow hash is not meant for). No third-party
  dependency for either.
- **Messaging, Jobs, Documents, Caching, Storage, Mailing** — one interface
  and one working implementation each, exactly as `docs/STRUCTURE.md`
  describes for this layer. Today's implementations are intentionally
  minimal and dependency-light rather than the full external integrations
  named in `docs/SPEC.md`'s dependency table:

  | Interface | Today's implementation | Real integration planned |
  |---|---|---|
  | `IMessagePublisher` | in-process queue, logged | RabbitMQ |
  | `IJobScheduler` | in-process background queue | Hangfire |
  | `IPdfGenerator` | hand-written minimal PDF writer | QuestPDF |
  | `IExcelExporter` | RFC 4180 CSV | ClosedXML |
  | `ICache` | in-memory (`IMemoryCache`) | Redis |
  | `IFileStorage` | local disk | + S3-compatible |
  | `IEmailSender` | SMTP when configured, logged otherwise | (same — SMTP is the real integration) |

  Every one of these is a genuine, tested implementation of its interface —
  none is a no-op — and every one is swappable for its planned counterpart
  without changing a caller, which is the point of depending on the
  interface rather than the class. See
  [docs/DEPENDENCIES.md](../../docs/DEPENDENCIES.md) for what is and is not
  a dependency of this backend yet.

## What the Identity feature does

- **`Domain`** — `User`, `Role`, `RefreshToken` and the `UserRole` link,
  each owning its own invariants (deactivating twice is a no-op, assigning
  a held role is a no-op, revoking an unheld role is treated as success —
  matching `contract/openapi.yaml` exactly). Five domain events. Zero
  package references and zero project references — genuinely depends on
  nothing, not merely "no framework imports": `Entity`, `IAuditable` and
  the domain-event marker interfaces are all defined inside this project
  rather than shared from `Shared`, specifically so this holds.
- **`Persistence`** — `IdentityDbContext` and every `IEntityTypeConfiguration<T>`,
  plus repository implementations. No `Npgsql` reference anywhere in this
  project — only `Microsoft.EntityFrameworkCore.Relational`, which is
  relational-generic, not provider-specific.
- **`Contracts`** — every DTO and request `contract/openapi.yaml` names for
  Identity, as plain records with zero dependencies — the only surface
  another feature (or `Endpoints`) may see.
- **`Application`** — one `Command` or `Query` per contract operation
  (register, login, refresh, logout, update profile, assign/revoke role,
  deactivate, get user, list users, list roles), each with its own handler
  in the same file — a reader follows one operation start to finish without
  jumping between projects. Every expected failure returns a `Result`
  carrying an `AppError`, never an exception. Mapping between entities and
  DTOs is hand-written, with a test per DTO asserting every property is
  populated.
- **`Endpoints`** — one minimal-API file per sub-area (`AuthEndpoints`,
  `UsersEndpoints`, `RolesEndpoints`), each mapping HTTP directly onto a
  Command or Query and a `Result` onto either the response DTO or the
  Problem envelope. Role-based authorization is a `permission` claim check
  (`RequirePermissionExtensions`) against the access token, not a hard-coded
  role name — a role is just a named bundle of permission codes.

## What `Database/Postgres` does

The only project allowed to reference `Npgsql.EntityFrameworkCore.PostgreSQL`
— see `docs/STRUCTURE.md`. `AddPostgresPersistence` wires `IdentityDbContext`
to a real connection string; `MigratePostgresDatabaseAsync` runs pending
migrations at startup; `PostgresSeeder` seeds two roles (`admin`, `user`)
and two accounts, idempotently (checked by the admin account's presence, so
an ordinary restart is a no-op, not a duplicate-key error).

## What `Host` does

The composition root: reads `ConnectionStrings:Postgres` and the `Jwt`
configuration section, wires every `Add...` extension the layers below
expose, configures JWT bearer authentication (`Host/Security/JwtAccessTokenIssuer`
implements Application's `IAccessTokenIssuer` port; `JwtProblemDetailsEvents`
rewrites the default bearer-auth 401/403 into the contract's Problem
envelope), configures Serilog to the console with the correlation ID
attached to every log line, exposes `/health/live` and `/health/ready` (the
latter checks Postgres connectivity), serves the authoritative OpenAPI contract
at `/openapi.yaml` and interactive Swagger UI documentation at `/docs` (driven
by `contract/openapi.yaml`, with no code-first generators such as Swashbuckle or NSwag),
and migrates and seeds the database on startup. Swagger UI itself is vendored in
`contract/docs-assets/swagger-ui`, compiled into the Host assembly and served at
`/docs/assets/` — the documentation page loads nothing from the internet, so it
renders on an air-gapped network and tells no third party who is reading it.

**The JWT signing key in `appsettings.Development.json` is a fixed, publicly
known development default** — stated plainly in `Host/Security/JwtOptions.cs`
and never used past local development; a real deployment supplies its own
secret via configuration or environment, and `appsettings.json`'s own
(production-facing) key is intentionally blank so a missing secret fails
loudly at startup rather than silently signing tokens with nothing.

## Architecture tests

`tests/ArchitectureTests` (NetArchTest) turns `docs/STRUCTURE.md`'s two hard
rules, plus the layering `docs/STRUCTURE.md`'s dependency table implies,
into six tests that fail the build on violation:

- `Domain` depends on nothing — checked at the raw assembly-reference
  level, not just "no framework imports": zero references to any other
  `StackBraid.*` assembly, EF Core, ASP.NET Core or Mediator.
- `Application` depends only on its own `Domain`, `Contracts` and `Shared`
  — never reaching forward into `Persistence` or `Endpoints`.
- `Persistence` depends only on its own `Domain` and `Shared` — never
  skipping forward into `Application` or `Endpoints`.
- `Contracts` depends on none of its own feature's internals (`Domain`,
  `Persistence`, `Application`, `Endpoints`) — the only surface another
  feature may see stays genuinely empty of everything else.
- `Shared` never imports a feature.
- No provider name appears outside `Database/Postgres` — checked by
  scanning every other assembly's references for `Npgsql`.

Every one of these six was seen to genuinely fail before being committed:
each rule was violated on purpose (an extra `ProjectReference` plus one
line of code touching the forbidden type), run in isolation, watched fail
with the violating type named in the output, then reverted — never
committed. This is not a claim taken on faith; the commit message for this
work names the exact violation tried for each rule.

## Building and testing locally

```bash
cd backends/dotnet
dotnet restore StackBraid.sln --locked-mode
dotnet build StackBraid.sln
dotnet test tests/Shared.UnitTests/StackBraid.Shared.UnitTests.csproj
dotnet test tests/Features.Identity.UnitTests/StackBraid.Features.Identity.UnitTests.csproj

# Integration tests need a real Postgres — see tests/Features.Identity.IntegrationTests/README.md
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
dotnet test tests/Features.Identity.IntegrationTests
./scripts/stop-local-postgres.sh
```

### Running the server and the conformance suite together

```bash
cd backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
ConnectionStrings__Postgres="$STACKBRAID_TEST_POSTGRES_CONNECTION_STRING" \
  Jwt__SigningKey="<any base64 32+ byte value for a local run>" \
  dotnet run --project src/Host --urls http://127.0.0.1:8080 &

cd ../../contract/conformance
CONFORMANCE_ADMIN_EMAIL="admin@stackbraid.local" CONFORMANCE_ADMIN_PASSWORD="ChangeMe!123" \
  node cli/run.mjs http://127.0.0.1:8080

cd ../../backends/dotnet && ./scripts/stop-local-postgres.sh
```

### Calling this from a browser-based frontend

No origin is trusted by default — a browser's cross-origin request (every
local frontend dev server, since it runs on a different port) is refused
until its origin is listed explicitly:

```bash
Cors__AllowedOrigins__0="http://127.0.0.1:3000" \
  dotnet run --project src/Host --urls http://127.0.0.1:8080
```

See [`frontends/nextjs/README.md`](../../frontends/nextjs/README.md) for a
frontend that actually depends on this.
