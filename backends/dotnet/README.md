# backends/dotnet — under construction

**Status: not a working backend yet.** `Shared` (cross-cutting plumbing) and
the Identity feature's domain, persistence and Postgres provider all
compile and are tested against a real database. Nothing serves HTTP yet —
no endpoints, no authentication — and nothing here has been run against the
contract's conformance suite (see [docs/ROADMAP.md](../../docs/ROADMAP.md),
step 2). Do not treat any file under this folder as evidence of a working
API.

```
backends/dotnet/
├── StackBraid.sln
├── Directory.Build.props    shared build settings (net10.0, nullable, per-project lock files)
├── scripts/                 start/stop a throwaway local Postgres for integration tests
├── src/
│   ├── Shared/               done — see below
│   ├── Database/Postgres/    Npgsql wiring, migrations, seed data — done, see below
│   ├── Features/Identity/
│   │   ├── Domain/            entities, value objects, domain events — depends on nothing
│   │   ├── Persistence/       DbContext, entity configuration, repositories — provider-agnostic
│   │   ├── Application/       scaffolded, empty — next
│   │   ├── Contracts/         scaffolded, empty — next
│   │   └── Endpoints/         scaffolded, empty — next
│   └── Host/                 scaffolded — starts, serves only /health/live
└── tests/
    ├── Shared.UnitTests/                 real tests, all passing
    ├── Features.Identity.UnitTests/      domain entity behaviour, all passing
    ├── Features.Identity.IntegrationTests/   real local Postgres, all passing — see its own README
    └── ArchitectureTests/
```

## What `Shared` actually does today

- **Persistence** — `AppDbContextBase`, a provider-agnostic `DbContext` base
  (a template-method save pipeline only — it knows nothing about any
  feature's entity types, so `Domain` genuinely depends on nothing) and
  `IRepository`/`RepositoryBase`/`IUnitOfWork` on top of it. No
  provider-specific type appears here — that is `Database/Postgres`'s job.
- **Web** — the RFC 9457 Problem envelope the contract requires
  (`ProblemDetailsMapper`, `GlobalExceptionHandler`), a correlation ID
  middleware, and the one rate-limit policy every backend needs on day one
  (`RateLimitingExtensions`).
- **Localization** — `IAppLocalizer`, backed by two embedded JSON catalogues
  (English and Spanish) — genuinely resolves per `Accept-Language`, not a
  stub.
- **Security** — `IPasswordHasher`, PBKDF2-HMAC-SHA256 at OWASP's current
  minimum (600,000 iterations), built on `Rfc2898DeriveBytes` — no
  third-party dependency.
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

## What the Identity feature's `Domain` and `Persistence` do today

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
  relational-generic, not provider-specific (see
  [docs/DEPENDENCIES.md](../../docs/DEPENDENCIES.md)).

## What `Database/Postgres` does today

The only project allowed to reference `Npgsql.EntityFrameworkCore.PostgreSQL`
— see `docs/STRUCTURE.md`. `AddPostgresPersistence` wires `IdentityDbContext`
to a real connection string; `MigratePostgresDatabaseAsync` runs pending
migrations at startup; `PostgresSeeder` seeds two roles (`admin`, `user`)
and two accounts, idempotently (checked by the admin account's presence, so
an ordinary restart is a no-op, not a duplicate-key error).

Proven against a real, throwaway local Postgres cluster — no Docker, no
Testcontainers (see `tests/Features.Identity.IntegrationTests/README.md`
for why, and `scripts/start-local-postgres.sh` for how): migrations run
from a genuinely empty database to fully current with no manual step,
every mapped table exists afterward, and seeding, search/pagination/sort,
role assignment and refresh-token rotation all round-trip correctly.

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
