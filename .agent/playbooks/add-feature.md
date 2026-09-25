# Playbook: Add Feature

This playbook describes the complete, end-to-end procedure for introducing a new domain feature slice into StackBraid.

## Core Rules

1. **The contract comes first.** An API never changes by editing a backend. The feature is specified in `contract/openapi.yaml`, clients are generated from it, and implementations follow.
2. **End-to-end across every stack.** A feature that cannot be done end-to-end in every shipped stack does not go in. Both backends (.NET and Python) and both web frontends (Next.js and Angular), plus mobile (Flutter) if a mobile surface applies, must implement the feature.
3. **Feature names are identical across all stacks.** If adding `invoices`, the folder is `invoices` everywhere — not `Invoice` in one stack and `billing` in another. Casing follows each language's convention, but the word is identical.
4. **Strict layering in every stack.**
   - `Domain/` (`domain/`): Pure models, rules, value objects, domain events, repository interfaces/protocols. Zero framework dependencies.
   - `Application/` (`application/`): Use cases (commands, queries, validators, DTO mappers). Depends only on Domain, Contracts, and Shared.
   - `Persistence/` (`data/`): Data access. Backend persists to database; frontend/mobile calls the generated API client.
   - `Endpoints/` (`presentation/`): Delivery surface. Minimal API / FastAPI endpoints in backends; UI screens/components in frontends and mobile.
5. **Enforced isolation.**
   - A feature never reaches into another feature's internals — only its `Contracts/`.
   - `Shared/` never imports a feature.
   - Database provider code lives only under `Database/<Provider>/` (backends).

---

## Step-by-Step Procedure

### Step 1: Name and Scope the Feature

1. Select a clear, singular or plural noun representing the domain capability (e.g., `invoices`, `notifications`, `tenants`).
2. Verify the chosen name is used consistently:
   - .NET Backend: `backends/dotnet/src/Features/<PascalName>/`
   - Python Backend: `backends/python/src/app/features/<snake_name>/`
   - Next.js: `frontends/nextjs/src/[web|admin]/features/<kebab-or-lower-name>/`
   - Angular: `frontends/angular/src/[web|admin]/features/<kebab-or-lower-name>/`
   - Flutter: `mobile/flutter/lib/features/<snake_name>/`

### Step 2: Define the OpenAPI Contract

1. Open `contract/openapi.yaml`.
2. Add a new tag under `tags:` describing the feature.
3. Define request and response schemas under `components/schemas/`:
   - Follow RFC 9457 for error envelopes (`Problem`).
   - Use offset pagination (`Page` + `<Feature>Page`) for paginated lists.
   - Use RFC 3339 UTC with a trailing `Z` for all timestamps (`UtcDateTime`).
4. Add endpoint paths under `paths:`, specifying operations, parameters, request bodies, and explicit error status codes (400, 401, 403, 404).
5. Validate the contract:
   ```bash
   npx @redocly/cli lint contract/openapi.yaml
   ```

### Step 3: Regenerate Clients

1. Run the client generation script:
   ```bash
   ./scripts/generate-clients.sh
   ```
2. Verify zero unexpected drift:
   ```bash
   ./scripts/check-client-drift.sh
   ```
3. Inspect generated artifacts:
   - TypeScript client: `clients/typescript/src/generated/`
   - Dart client: `clients/dart/lib/src/`

### Step 4: Implement .NET Backend

Under `backends/dotnet/src/Features/<FeatureName>/`:

1. **Contracts** (`StackBraid.Features.<FeatureName>.Contracts`):
   - Define public request records and DTOs that external callers and other features may reference.
2. **Domain** (`StackBraid.Features.<FeatureName>.Domain`):
   - Define entity classes inheriting from `Entity<TId>` or implementing `IAuditable`.
   - Define domain events and domain repository interfaces (`I<Entity>Repository`).
   - Keep zero framework dependencies (no EF Core, no ASP.NET).
3. **Application** (`StackBraid.Features.<FeatureName>.Application`):
   - Define Commands and Queries implementing mediator request types.
   - Implement handlers, FluentValidation validators, and mapping extensions.
4. **Persistence** (`StackBraid.Features.<FeatureName>.Persistence`):
   - Implement EF Core provider-agnostic entity configurations (`IEntityTypeConfiguration<T>`).
   - Implement repositories using EF Core DbContext.
5. **Endpoints** (`StackBraid.Features.<FeatureName>.Endpoints`):
   - Implement route mapping extensions (`Map<FeatureName>Endpoints`) extending `IEndpointRouteBuilder`.
   - Return RFC 9457 `ProblemDetails` on failures.
6. **Host & Database Integration**:
   - Register services and map endpoints in `backends/dotnet/src/Host/Program.cs`.
   - Add database provider migrations in `backends/dotnet/src/Database/<Provider>/Migrations/` (see `add-migration` playbook).
   - Write unit and integration tests under `backends/dotnet/tests/`.

### Step 5: Implement Python Backend

Under `backends/python/src/app/features/<feature_name>/`:

1. **contracts/**: Define Pydantic request models and response DTOs using `CamelModel` for automatic camelCase JSON serialization.
2. **domain/**: Define domain dataclasses, value objects, and repository `Protocol` interfaces.
3. **application/**: Implement use cases, command/query handlers, and business logic.
4. **persistence/**: Define SQLAlchemy declarative models and repository implementations.
5. **endpoints/**: Define FastAPI `APIRouter` with path operations, auth dependencies, and status codes.
6. **Host & Database Integration**:
   - Include router in `backends/python/src/app/host/main.py`.
   - Add Alembic migrations under `backends/python/src/app/database/<provider>/migrations/`.
   - Write tests under `backends/python/tests/`.

### Step 6: Implement Frontends and Mobile

1. **Next.js** (`frontends/nextjs/src/[web|admin]/features/<feature>/`):
   - `domain/`: View models, filter types.
   - `data/`: Data access calling `@stackbraid/client-typescript`.
   - `application/`: TanStack Query hooks (`useQuery`, `useMutation`).
   - `presentation/`: React components and pages using shadcn/ui.
   - Route entry: `frontends/nextjs/src/app/(web|admin)/.../page.tsx`.
2. **Angular** (`frontends/angular/src/[web|admin]/features/<feature>/`):
   - `domain/`: TypeScript interfaces and validation rules.
   - `data/`: Angular injectable repositories calling the generated client.
   - `application/`: State management / TanStack Query services.
   - `presentation/`: Angular components using Angular Material.
   - Route entry: `frontends/angular/src/app/app.routes.ts`.
3. **Flutter** (`mobile/flutter/lib/features/<feature>/`):
   - `domain/`: Dart entities and validation.
   - `data/`: Repository consuming `package:stackbraid_client`.
   - `application/`: Use cases and controllers.
   - `presentation/`: Flutter widgets using Material 3.

### Step 7: Add Localized Strings

1. Add translation keys for both `en` and `es` in:
   - Backend JSON catalogues: `Shared/Localization/Resources/`
   - Frontend feature message directories: `presentation/messages/`
   - Mobile string files: `presentation/i18n/`
2. Verify completeness across all catalogues:
   ```bash
   node scripts/check-translation-keys.mjs
   ```

### Step 8: Verify Conformance and Architecture

1. Start throwaway local Postgres (no Docker):
   ```bash
   export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./backends/dotnet/scripts/start-local-postgres.sh)"
   ```
2. Run backend under test, then run contract conformance:
   ```bash
   node contract/conformance/cli/run.mjs http://localhost:8080
   ```
3. Run architecture tests:
   ```bash
   dotnet test backends/dotnet/tests/ArchitectureTests
   (cd backends/python && uv run lint-imports)
   (cd frontends/nextjs && npx depcruise --config .dependency-cruiser.mjs src)
   (cd frontends/angular && npx depcruise --config .dependency-cruiser.mjs src)
   ```
4. Stop local Postgres:
   ```bash
   ./backends/dotnet/scripts/stop-local-postgres.sh
   ```
