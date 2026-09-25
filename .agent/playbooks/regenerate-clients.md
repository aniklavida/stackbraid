# Playbook: Regenerate Clients

This playbook describes the procedure for regenerating both TypeScript and Dart API clients from `contract/openapi.yaml` and mechanically proving zero drift.

## Core Rules

1. **Never hand-edit generated code.** `clients/typescript/src/generated/` and `clients/dart/lib/src/` are generated artifacts. Hand-edits will be overwritten and are rejected by CI.
2. **The contract owns the truth.** If a client needs a new method, type, or field, update `contract/openapi.yaml` first, then regenerate.
3. **Both clients are regenerated together.** Never regenerate only one client. Both must stay aligned with the contract.

---

## Toolchain Requirements

- **Node.js** >= 18.17 and `npm`
- **Dart SDK** >= 3.11 (required for `build_runner` and `json_serializable` code generation in `clients/dart`)

---

## Step-by-Step Procedure

### Step 1: Run the Regeneration Script

From the repository root:
```bash
./scripts/generate-clients.sh
```

This single command performs the following operations:
1. Validates the `dart` and `npm` toolchain versions.
2. Changes to `clients/typescript`, installs dev dependencies, and invokes `@hey-api/openapi-ts` to generate TypeScript types and SDK methods.
3. Changes to `clients/dart`, invokes `@openapitools/openapi-generator-cli` with `openapi-generator-config.yaml`, runs `dart pub get`, and executes `dart run build_runner build` to produce serialization logic.

### Step 2: Review the Changes

Check git status and diff:
```bash
git status clients/
git diff clients/
```

Verify that the changes in `clients/` reflect only the modifications made to `contract/openapi.yaml`.

### Step 3: Run the Drift Check

Execute the automated drift detection script:
```bash
./scripts/check-client-drift.sh
```

This script:
1. Regenerates both clients from scratch.
2. Differs the freshly generated output against the git index and working tree.
3. Asserts that no tracked files were modified and no untracked files were created.
4. Executes the project picker matrix test (`check-create-picker.mjs`) to confirm template generation remains reproducible.

### Step 4: Verify Consumers Build Cleanly

Check that consuming frontends and mobile compile against the new client interfaces:

1. **TypeScript Client Typecheck**:
   ```bash
   (cd clients/typescript && npm run typecheck)
   ```
2. **Next.js Frontend Build**:
   ```bash
   (cd frontends/nextjs && npm run build)
   ```
3. **Angular Frontend Build**:
   ```bash
   (cd frontends/angular && npm run build)
   ```
4. **Dart Client Tests**:
   ```bash
   (cd clients/dart && dart test)
   ```

### Step 5: Stage and Commit

Stage both the contract and the updated client code:
```bash
git add contract/openapi.yaml clients/
```

If the pre-commit hook is active (`git config core.hooksPath .githooks`), it will automatically verify the generated clients before allowing the commit.
