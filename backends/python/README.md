# backends/python

**Status: the second working backend.** Register, log in, refresh, log out,
manage users and roles — all implemented, all authenticated with real JWTs,
all backed by a real Postgres database, and **all proven green against
`contract/conformance`, unchanged from the run against the .NET backend**
(see "Conformance" below for the real run). `import-linter` enforces
`docs/STRUCTURE.md`'s dependency rules, not just convention — a violation
fails `lint-imports`, not merely a review comment (see "Architecture" below).
This is `docs/ROADMAP.md` step 3's target. What is not yet true: no second
provider (SQL Server/MySQL), no second backend feature, and none of the
Tier 2 "professional" capabilities beyond the minimal defaults `shared/`
already ships (see below). Frontends and mobile do not exist yet.

```
backends/python/
├── pyproject.toml            dependencies, pytest config, import-linter contracts
├── alembic.ini
├── requirements-lock.txt     uv pip freeze — the exact resolved graph, audited
├── scripts/                  start/stop a throwaway local Postgres for integration tests
├── src/app/
│   ├── shared/                cross-cutting plumbing — see below
│   ├── database/postgres/     asyncpg wiring, Alembic migrations, seed data — see below
│   ├── features/identity/
│   │   ├── domain/            entities, value objects, domain events — depends on nothing
│   │   ├── persistence/       SQLAlchemy models, repositories — provider-agnostic
│   │   ├── contracts/         Pydantic DTOs and requests mirroring contract/openapi.yaml
│   │   ├── application/       every command and query the contract needs
│   │   └── endpoints/         the HTTP surface — FastAPI routers, permission-gated
│   └── host/                  composition root — JWT, settings, DI wiring, migrations/seed on startup
└── tests/
    ├── shared/                       real tests, all passing
    ├── features/identity/{domain,application,mapping}/   all passing
    ├── integration/                  real local Postgres, all passing
    └── architecture/                 placeholder — the layering rule lives in pyproject.toml, enforced by import-linter
```

## The only intended difference from .NET

.NET dispatches commands through a source-generated mediator; **this backend
resolves handlers directly from FastAPI's own dependency graph** — no
mediator library. Same folders, same names, same flow, each idiomatic (see
`docs/STRUCTURE.md`).

## Conformance — the real evidence

```
$ CONFORMANCE_ADMIN_EMAIL="admin@stackbraid.local" CONFORMANCE_ADMIN_PASSWORD="<seeded>" \
    CONFORMANCE_MAX_EXPIRY_WAIT_MS=65000 node cli/run.mjs http://127.0.0.1:<port>

34 passed, 0 failed, 0 skipped, 34 total.
```

Run against a genuinely fresh, empty local Postgres cluster (no Docker, no
Testcontainers — see `scripts/start-local-postgres.sh`) migrated and seeded
by this backend's own startup path, with a short-lived (5-second) access
token configured (`STACKBRAID_JWT_ACCESS_TOKEN_LIFETIME_SECONDS`) so the
suite's expiry check exercises a real expiry rather than skipping it. Every
check in `contract/conformance` passes: schema shape, the RFC 9457 Problem
envelope on every documented error, offset pagination arithmetic,
`UtcDateTime`'s exact `Z`-suffixed format, both token-delivery paths (body
and httpOnly cookie) for login/refresh/logout, refresh-token rotation and
revocation, and permission-gated admin endpoints. Both generated clients
(`clients/typescript`, `clients/dart`) called this backend successfully with
no hand edits — register, log in, and read the caller's own account,
exercised directly against a running instance, with only the base URL
changed from the .NET run.

One real bug this run caught and fixed: Pydantic's `EmailStr` pulls in
`email-validator`'s deliverability/special-use-domain checks, which reject a
`.local` address outright — locking the seeded `admin@stackbraid.local`
account out of its own login endpoint. Replaced with a plain pattern
matching the domain's own `Email` value object and the contract's
`format: email`, removing a dependency in the process.

## What `shared/` does

- **`persistence`** — `OrmBase` (the one shared `DeclarativeBase` every
  feature's SQLAlchemy models register against) and `UnitOfWork` on top of
  a plain `AsyncSession`. No provider-specific type appears here — that is
  `database/postgres/`'s job.
- **`web`** — the RFC 9457 Problem envelope the contract requires
  (`errors.py`, `problem.py`, `exception_handling.py`), a correlation-ID
  middleware, and the one rate-limit policy every backend needs on day one
  (`rate_limit.py`, an in-process fixed-window counter).
- **`localization`** — `AppLocalizer`, backed by two embedded JSON
  catalogues (English and Spanish) — genuinely resolves per
  `Accept-Language`, not a stub.
- **`security`** — `Pbkdf2PasswordHasher` (PBKDF2-HMAC-SHA256, OWASP's
  current minimum, built on the standard library's `hashlib`) and
  `opaque_token` (a fast SHA-256 hash for refresh tokens — deliberately not
  PBKDF2, which would punish the read-heavy per-request lookup a slow hash
  is not meant for). No third-party dependency for either.
- **`messaging`, `jobs`, `documents`, `caching`, `storage`, `mailing`** —
  one Protocol and one working implementation each, exactly as
  `docs/STRUCTURE.md` describes for this layer, mirroring the .NET
  backend's own minimal defaults:

  | Protocol | Today's implementation | Real integration planned |
  |---|---|---|
  | `MessagePublisher` | in-process `asyncio.Queue`, logged | RabbitMQ |
  | `JobScheduler` | in-process background queue | a real job runner |
  | `PdfGenerator` | hand-written minimal PDF writer | a real PDF library |
  | `ExcelExporter` | RFC 4180 CSV | a real Excel library |
  | `Cache` | in-process, per-key TTL | Redis |
  | `FileStorage` | local disk | + S3-compatible |
  | `EmailSender` | SMTP when configured, logged otherwise | (same — SMTP is the real integration) |

  Every one of these is a genuine, tested implementation of its Protocol —
  none is a no-op — and every one is swappable for its planned counterpart
  without changing a caller. See [docs/DEPENDENCIES.md](../../docs/DEPENDENCIES.md)
  for what is and is not a dependency of this backend yet.

## What the Identity feature does

- **`domain`** — `User`, `Role`, `RefreshToken` and the `UserRole` link,
  each owning its own invariants (deactivating twice is a no-op, assigning
  a held role is a no-op, revoking an unheld role is treated as success —
  matching `contract/openapi.yaml` exactly, and matching the .NET domain's
  behaviour). Five domain events. Imports nothing outside this package and
  the standard library — genuinely depends on nothing, enforced by
  `import-linter`, not merely documented.
- **`persistence`** — SQLAlchemy models (`models.py`) mapped to and from
  the domain entities by the repositories (`repositories.py`); the domain
  never sees a SQLAlchemy object. One deliberate ORM detail worth recording:
  `RefreshTokenModel.user` carries an explicit `relationship()`, not just
  the column-level `ForeignKey` — SQLAlchemy only orders `INSERT`
  statements across two mapped classes by their foreign-key dependency when
  an ORM relationship links them, so a token and its brand-new user added
  in the same flush could otherwise hit the constraint before either
  commits.
- **`contracts`** — Pydantic DTOs and requests, camelCase on the wire via
  one shared `CamelModel` base, and a `UtcDateTime` type that always
  serializes with a trailing `Z` (`datetime_utils.py`) rather than Python's
  own `+00:00` default — the same divergence a spike caught between the two
  backends before either shipped.
- **`application`** — one handler class per contract operation (11 total),
  hand-written mapping (`mapping.py`) with its own test file. Login/refresh
  depend on `AccessTokenIssuer`, a Protocol with its real implementation at
  the edge (`endpoints/security.py`), not in `application/` — the port is
  declared here, the JWT-specific implementation lives where the token is
  also verified on the way back in.
- **`endpoints`** — FastAPI routers per contract's `Auth`/`Users`/`Roles`
  tags, `require_permission(...)` dependencies gating admin routes, and the
  httpOnly `refreshToken` cookie (`HttpOnly; Secure; SameSite=Strict;
  Path=/v1/auth`) alongside the body token every login/refresh returns.

## Architecture — enforced, not just documented

Four `import-linter` contracts, defined in `pyproject.toml`:

1. **`shared` never imports a feature.**
2. **The Identity feature's own layers** — `endpoints` may depend on
   `persistence`, `application` and `domain`; `persistence` may depend on
   `application` and `domain`; `application` may depend only on `domain`.
3. **The Identity domain depends on nothing** — not `shared`, not
   `sqlalchemy`, not `pydantic`, not `fastapi`, not this feature's own
   `application`/`persistence`/`contracts`/`endpoints`.
4. **Contracts depend on nothing feature-internal** — not this feature's
   own `domain`/`application`/`persistence`/`endpoints`.

Every one of these was seen to genuinely fail before being trusted: each
rule was violated on purpose (one import added to `domain/entities.py`
reaching into `shared`), run in isolation, watched fail with the violating
import named in the output, then reverted — never committed.

## Building and testing locally

```bash
cd backends/python
uv venv --python 3.12 .venv
source .venv/bin/activate
uv pip install -e ".[dev]"

# Unit tests — no database needed
PYTHONPATH=src pytest tests/shared tests/features

# Architecture rules
cd src && lint-imports --config ../pyproject.toml

# Integration tests need a real Postgres
cd backends/python
export STACKBRAID_POSTGRES_DSN="$(./scripts/start-local-postgres.sh)"
PYTHONPATH=src pytest tests/integration
./scripts/stop-local-postgres.sh
```

### Running the server and the conformance suite together

```bash
cd backends/python
export STACKBRAID_POSTGRES_DSN="$(./scripts/start-local-postgres.sh)"
STACKBRAID_JWT_ACCESS_TOKEN_LIFETIME_SECONDS=5 \
  PYTHONPATH=src uvicorn app.host.main:app --port 8080 &

cd ../../contract/conformance
CONFORMANCE_ADMIN_EMAIL="admin@stackbraid.local" CONFORMANCE_ADMIN_PASSWORD="ChangeMe!123" \
  CONFORMANCE_MAX_EXPIRY_WAIT_MS=65000 node cli/run.mjs http://127.0.0.1:8080

cd ../../backends/python && ./scripts/stop-local-postgres.sh
```

Migrations and seeding run automatically on startup
(`STACKBRAID_RUN_MIGRATIONS_ON_STARTUP` / `STACKBRAID_SEED_ON_STARTUP`,
both default `true`) — no manual step between starting the server and
calling it.
