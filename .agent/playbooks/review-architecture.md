# Playbook: Review Architecture

This playbook describes how to review, audit, and mechanically verify architectural layering and module boundary invariants across StackBraid.

## Core Architectural Invariants

Every stack in StackBraid enforces strict boundaries:

1. **Domain knows nothing.** The `Domain/` (`domain/`) layer contains pure business models and validation logic. It carries zero framework dependencies (no EF Core, no ASP.NET, no SQLAlchemy, no Pydantic).
2. **Application knows only Domain, Contracts, and Shared.** The `Application/` (`application/`) layer orchestrates use cases. It must never reach into Persistence or Endpoints.
3. **Data/Persistence and Delivery/Endpoints sit on the outside.** They depend inward on Application and Domain. Nothing points inward from the outside edge.
4. **Shared never imports a feature.** The moment `Shared/` imports code from a domain feature, it ceases to be shared and becomes a hidden coupling point.
5. **Features communicate only through Contracts.** A feature never accesses another feature's internal entities, repositories, or services. Cross-feature interaction occurs exclusively via public DTOs, requests, and interfaces published in `Contracts/`.
6. **No provider name appears outside `Database/<Provider>/`.** Provider drivers (Postgres, SQL Server, MySQL) are strictly isolated. The composition root (`Host/`) is the only place a provider is chosen.
7. **Generated clients are never hand-edited.** `clients/` and `api/` folders are generated strictly from `contract/openapi.yaml`.

---

## Mechanical Verification Suites

StackBraid enforces these rules in CI using automated architecture testing tools:

### 1. .NET Backend (NetArchTest)

Located in `backends/dotnet/tests/ArchitectureTests/LayeringRulesTests.cs`.

**Execution:**
```bash
dotnet test backends/dotnet/tests/ArchitectureTests
```

**Enforced assertions:**
- `Domain_depends_on_nothing`: Fails if Domain references any non-BCL assembly.
- `Application_depends_only_on_its_own_Domain_Contracts_and_Shared`: Fails if Application references Persistence or Endpoints.
- `Persistence_depends_only_on_its_own_Domain_and_Shared`: Fails if Persistence reaches forward into Application or Endpoints.
- `Contracts_is_the_only_surface_another_feature_may_see`: Fails if Contracts references feature internals.
- `Shared_never_imports_a_feature`: Fails if Shared references any namespace under `StackBraid.Features`.
- `No_provider_name_appears_outside_Database`: Fails if non-Database/Host assemblies reference Npgsql, Pomelo, or Microsoft.Data.SqlClient.

### 2. Python Backend (Import Linter)

Configured in `backends/python/pyproject.toml` under `[tool.importlinter]`.

**Execution:**
```bash
cd backends/python
uv run lint-imports
```

**Enforced contracts:**
- `Shared never imports a feature`: Forbids `app.shared` from importing `app.features`.
- `Identity feature layers`: Enforces layer ordering (`endpoints` -> `persistence` -> `application` -> `domain`).
- `Identity domain depends on nothing`: Forbids `domain` from importing `app.shared`, `app.database`, `sqlalchemy`, `pydantic`, or `fastapi`.
- `Features and shared never import a database provider`: Forbids `app.shared` and `app.features` from importing `app.database`.

### 3. Next.js Frontend (Dependency Cruiser)

Configured in `frontends/nextjs/.dependency-cruiser.mjs`.

**Execution:**
```bash
cd frontends/nextjs
npx depcruise --config .dependency-cruiser.mjs src
```

**Enforced rules:**
- `no-cross-feature-imports`: Prevents features under `web/features` and `admin/features` from importing each other's internal files.
- `shared-cannot-import-features`: Prohibits `src/shared` from importing from `web` or `admin`.
- `web-admin-isolation`: Prohibits `src/web` from importing directly from `src/admin` and vice versa.

### 4. Angular Frontend (Dependency Cruiser)

Configured in `frontends/angular/.dependency-cruiser.mjs`.

**Execution:**
```bash
cd frontends/angular
npx depcruise --config .dependency-cruiser.mjs src
```

---

## Review Checklist for Pull Requests

Before submitting or approving a pull request:
- [ ] Run all four architecture test suites above; ensure zero violations.
- [ ] Check git diff for unauthorized cross-feature imports.
- [ ] Verify that new entity mappings in Persistence use provider-agnostic relational methods, not provider-specific SQL dialect calls.
- [ ] Check that no hand-edits have been introduced into `clients/` or `api/`.
