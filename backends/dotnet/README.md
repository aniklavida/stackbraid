# backends/dotnet — under construction

**Status: not a working backend yet.** `Shared` — the cross-cutting plumbing
every feature depends on (see [docs/STRUCTURE.md](../../docs/STRUCTURE.md)) —
compiles and is unit-tested. Nothing serves HTTP, nothing talks to a
database, and nothing here has been run against the contract's conformance
suite yet (see [docs/ROADMAP.md](../../docs/ROADMAP.md), step 2). Do not
treat any file under this folder as evidence of a working API.

```
backends/dotnet/
├── StackBraid.sln
├── Directory.Build.props    shared build settings (net10.0, nullable, per-project lock files)
├── src/
│   ├── Shared/               done — see below
│   ├── Database/Postgres/    scaffolded, empty
│   ├── Features/Identity/    scaffolded, empty
│   └── Host/                 scaffolded — starts, serves only /health/live
└── tests/
    ├── Shared.UnitTests/         real tests, all passing
    ├── Features.Identity.UnitTests/
    ├── Features.Identity.IntegrationTests/
    └── ArchitectureTests/
```

## What `Shared` actually does today

- **Persistence** — `AppDbContextBase`, a provider-agnostic `DbContext` base that
  stamps `IAuditable` timestamps and flushes queued `IHasDomainEvents` after a
  commit; `IRepository`/`RepositoryBase` and `IUnitOfWork` on top of it. No
  provider-specific type appears here — that is `Database/Postgres`'s job
  once it exists.
- **Web** — the RFC 9457 Problem envelope the contract requires
  (`ProblemDetailsMapper`, `GlobalExceptionHandler`), a correlation ID
  middleware, and the one rate-limit policy every backend needs on day one
  (`RateLimitingExtensions`).
- **Localization** — `IAppLocalizer`, backed by two embedded JSON catalogues
  (English and Spanish) — genuinely resolves per `Accept-Language`, not a
  stub.
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

## Building and testing locally

```bash
cd backends/dotnet
dotnet restore StackBraid.sln --locked-mode
dotnet build StackBraid.sln
dotnet test tests/Shared.UnitTests/StackBraid.Shared.UnitTests.csproj
```
