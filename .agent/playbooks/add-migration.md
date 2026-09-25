# Playbook: Add Migration

This playbook describes the procedure for adding database migrations across all three supported database providers (PostgreSQL, SQL Server, MySQL) in both .NET and Python backends.

## Core Rules

1. **Migrations are provider-specific.** The SQL generated for PostgreSQL, SQL Server, and MySQL differs substantially. One migrations folder cannot serve all three providers.
2. **Providers live in `Database/<Provider>/` only.** No database provider name may appear in `Shared/Persistence` or any feature's persistence layer.
3. **Keep migrations in sync.** Whenever entity configurations change, migrations must be generated for all three database providers in both backends.

---

## Step-by-Step Procedure

### Step 1: Generate .NET EF Core Migrations

StackBraid's .NET solution maintains dedicated migration assemblies and design-time DbContext factories under `backends/dotnet/src/Database/<Provider>/`:

```
backends/dotnet/src/Database/
├── Postgres/Migrations/
├── SqlServer/Migrations/
└── MySql/Migrations/
```

Navigate to `backends/dotnet`:
```bash
cd backends/dotnet
```

1. **PostgreSQL**:
   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/Database/Postgres \
     --startup-project src/Host \
     --context IdentityDbContext
   ```
2. **SQL Server**:
   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/Database/SqlServer \
     --startup-project src/Host \
     --context IdentityDbContext
   ```
3. **MySQL**:
   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/Database/MySql \
     --startup-project src/Host \
     --context IdentityDbContext
   ```

Review the generated migration classes and designer snapshots in each provider's `Migrations/` directory.

### Step 2: Generate Python Alembic Migrations

StackBraid's Python backend maintains separate Alembic migration version folders per provider:

```
backends/python/src/app/database/
├── postgres/migrations/versions/
├── sqlserver/migrations/versions/
└── mysql/migrations/versions/
```

Navigate to `backends/python`:
```bash
cd backends/python
```

1. **PostgreSQL** (uses root `alembic.ini`):
   ```bash
   alembic revision --autogenerate -m "<migration_name>"
   ```
2. **SQL Server** (uses SQL Server `alembic.ini`):
   ```bash
   alembic -c src/app/database/sqlserver/alembic.ini revision --autogenerate -m "<migration_name>"
   ```
3. **MySQL** (uses MySQL `alembic.ini`):
   ```bash
   alembic -c src/app/database/mysql/alembic.ini revision --autogenerate -m "<migration_name>"
   ```

Review the newly generated migration script in each provider's `migrations/versions/` directory. Check for correct data types (e.g., UUID representations, datetime with timezones).

### Step 3: Verify Migrations Locally (No Docker)

Test that the migrations apply cleanly against a live database using the repository's no-Docker throwaway Postgres script:

#### .NET Backend
```bash
cd backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"

# Run integration tests which automatically migrate the test database
dotnet test tests/Features.Identity.IntegrationTests

# Tear down throwaway cluster
./scripts/stop-local-postgres.sh
```

#### Python Backend
```bash
cd backends/python
export STACKBRAID_POSTGRES_DSN="$(./scripts/start-local-postgres.sh)"

# Run integration tests which apply migrations and verify schema
pytest tests/integration

# Tear down throwaway cluster
./scripts/stop-local-postgres.sh
```

### Step 4: Verify Database Provider Isolation

Run the architecture tests to confirm no provider-specific package has leaked outside `Database/<Provider>/`:
```bash
dotnet test backends/dotnet/tests/ArchitectureTests
(cd backends/python && uv run lint-imports)
```
