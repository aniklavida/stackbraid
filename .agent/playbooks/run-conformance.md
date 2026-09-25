# Playbook: Run Conformance

This playbook describes how to execute the contract conformance suite against any running backend in StackBraid.

## Core Rules

1. **One suite, zero backend bias.** The conformance suite lives in `contract/conformance/` and takes a base URL. It knows nothing about .NET, Python, or which database is running.
2. **Every backend must pass.** If a contract check fails, either the backend does not conform, or the contract was edited without updating the implementation. A backend cannot ship if it fails conformance.
3. **Zero external dependencies.** The conformance suite uses Node.js built-ins (`fetch`) and a hand-rolled schema validator. It carries no third-party libraries to audit.

---

## What the Suite Checks

- **Schema validation:** Every field in every response payload is checked against `contract/openapi.yaml`. Extra, missing, or mistyped fields are flagged as violations.
- **Problem Details envelope:** Error responses must conform to RFC 9457 (`application/problem+json`) with standard extensions (`code`, `traceId`) and validation error maps (`errors`).
- **Pagination:** Offset-based pagination (`page`, `pageSize`, `totalItems`, `totalPages`) is verified. Leaking cursor fields (`nextCursor`) is rejected.
- **Timestamp formatting:** All `UtcDateTime` values must strictly match RFC 3339 UTC with a trailing `Z` (e.g. `2026-09-25T14:00:00Z`). Numeric offsets (`+00:00`) fail validation.
- **Authentication lifecycle:** Exercises registration, login, token refresh, token rotation, logout revocation, and expiration behavior.
- **Token delivery:** Tests both request-body delivery (mobile/API clients) and httpOnly cookie delivery (`HttpOnly; Secure; SameSite=Strict`).
- **Realtime message payloads:** Probes SignalR hubs (.NET) and WebSockets (Python), validating event schemas over the wire.

---

## Step-by-Step Procedure

### Step 1: Start a Throwaway Test Database (No Docker)

```bash
# For .NET:
cd backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"

# For Python:
cd backends/python
export STACKBRAID_POSTGRES_DSN="$(./scripts/start-local-postgres.sh)"
```

### Step 2: Start the Backend Under Test

In a dedicated terminal, launch the backend:

#### .NET
```bash
cd backends/dotnet/src/Host
ASPNETCORE_URLS="http://127.0.0.1:8080" dotnet run
```

#### Python
```bash
cd backends/python
uvicorn app.host.main:app --host 127.0.0.1 --port 8080
```

Wait until the server is listening and healthy at `http://127.0.0.1:8080/health` (or `/v1/auth/login`).

### Step 3: Run the Conformance Runner

From the repository root:
```bash
node contract/conformance/cli/run.mjs http://127.0.0.1:8080
```

#### With Admin Credentials
To run permission-gated checks (user management, role assignment):
```bash
CONFORMANCE_ADMIN_EMAIL=admin@stackbraid.dev \
CONFORMANCE_ADMIN_PASSWORD=AdminPassword123! \
node contract/conformance/cli/run.mjs http://127.0.0.1:8080
```

### Step 4: Proving Suite Rigor (Demo Violations)

To verify that the suite genuinely catches defects and is not a false-positive pass, run the stub violation demo:
```bash
cd contract/conformance
npm run demo:violations
```

This starts `fixtures/stub-server/` and systematically injects five violation classes (wrong type, missing field, wrong timestamp format, wrong error shape, wrong pagination), asserting that each violation causes a test failure.

### Step 5: Teardown

1. Terminate the backend server.
2. Stop and clean up the throwaway Postgres cluster:
   ```bash
   ./scripts/stop-local-postgres.sh
   ```
