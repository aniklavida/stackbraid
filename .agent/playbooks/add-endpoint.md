# Playbook: Add Endpoint

This playbook describes the contract-first procedure for adding a new HTTP endpoint to an existing feature in StackBraid.

## Core Rules

1. **Never edit a backend first.** Every API endpoint originates in `contract/openapi.yaml`. Backends implement the contract; clients are generated from it.
2. **Every endpoint specifies its error responses.** An endpoint that documents only status 200/201 teaches clients to only handle success. All non-2xx responses must return RFC 9457 Problem Details (`application/problem+json`) with standard `code` and `traceId` extensions.
3. **Use standard conventions:**
   - Offset pagination (`page`, `pageSize`, returning `<Schema>Page`) for paginated lists.
   - Timestamps formatted as RFC 3339 UTC with a trailing `Z` (`UtcDateTime`).
   - Standard path prefixing starting with `/v1/`.

---

## Step-by-Step Procedure

### Step 1: Update the OpenAPI Contract

1. Open `contract/openapi.yaml`.
2. Locate or define the path under `paths:`:
   ```yaml
   /v1/users/{id}/status:
     patch:
       tags:
         - Users
       summary: Update user status
       description: Activates or deactivates a user account. Requires administrative privileges.
       operationId: updateUserStatus
       security:
         - bearerAuth: []
       parameters:
         - name: id
           in: path
           required: true
           schema:
             type: string
             format: uuid
       requestBody:
         required: true
         content:
           application/json:
             schema:
               $ref: '#/components/schemas/UpdateUserStatusRequest'
       responses:
         '200':
           description: Status updated successfully.
           content:
             application/json:
               schema:
                 $ref: '#/components/schemas/User'
         '400':
           description: Invalid request payload.
           content:
             application/problem+json:
               schema:
                 $ref: '#/components/schemas/Problem'
         '401':
           description: Authentication required.
           content:
             application/problem+json:
               schema:
                 $ref: '#/components/schemas/Problem'
         '403':
           description: Insufficient permissions.
           content:
             application/problem+json:
               schema:
                 $ref: '#/components/schemas/Problem'
         '404':
           description: User not found.
           content:
             application/problem+json:
               schema:
                 $ref: '#/components/schemas/Problem'
   ```
3. Add any new request or response schemas under `components/schemas/`.
4. Validate the contract with Redocly:
   ```bash
   npx @redocly/cli lint contract/openapi.yaml
   ```

### Step 2: Regenerate Generated Clients

1. Run the client generator:
   ```bash
   ./scripts/generate-clients.sh
   ```
2. Verify clients generated cleanly with no drift:
   ```bash
   ./scripts/check-client-drift.sh
   ```

### Step 3: Implement in .NET Backend

Under `backends/dotnet/src/Features/<Feature>/`:

1. **Contracts (`Contracts/Requests/` or `Contracts/Dtos/`)**:
   - Define the request record:
     ```csharp
     public sealed record UpdateUserStatusRequest(string Status);
     ```
2. **Application (`Application/Commands/` or `Application/Queries/`)**:
   - Create Command/Query record implementing mediator request:
     ```csharp
     public sealed record UpdateUserStatusCommand(Guid Id, string Status) : IRequest<Result<UserDto>>;
     ```
   - Implement handler and validator using FluentValidation.
3. **Endpoints (`Endpoints/`)**:
   - Map route in the feature's endpoint class:
     ```csharp
     app.MapPatch("/v1/users/{id:guid}/status", async (
         Guid id,
         UpdateUserStatusRequest request,
         IMediator mediator,
         CancellationToken ct) =>
     {
         var result = await mediator.Send(new UpdateUserStatusCommand(id, request.Status), ct);
         return result.Match(
             user => Results.Ok(user),
             error => Results.Problem(error.ToProblemDetails()));
     })
     .WithName("UpdateUserStatus")
     .Produces<UserDto>(StatusCodes.Status200OK)
     .ProducesProblem(StatusCodes.Status400BadRequest)
     .ProducesProblem(StatusCodes.Status401Unauthorized)
     .ProducesProblem(StatusCodes.Status403Forbidden)
     .ProducesProblem(StatusCodes.Status404NotFound)
     .RequireAuthorization("AdminOnly");
     ```
4. Write unit tests in `tests/Features.<Feature>.UnitTests/`.

### Step 4: Implement in Python Backend

Under `backends/python/src/app/features/<feature>/`:

1. **Contracts (`contracts/requests.py` / `dtos.py`)**:
   - Define Pydantic request model:
     ```python
     from app.features.identity.contracts.camel import CamelModel

     class UpdateUserStatusRequest(CamelModel):
         status: str
     ```
2. **Application (`application/commands/` or `queries/`)**:
   - Implement command handler processing domain logic.
3. **Endpoints (`endpoints/`)**:
   - Map endpoint in FastAPI router:
     ```python
     @router.patch(
         "/v1/users/{user_id}/status",
         response_model=UserDto,
         responses={
             400: {"model": Problem},
             401: {"model": Problem},
             403: {"model": Problem},
             404: {"model": Problem},
         },
     )
     async def update_user_status(
         user_id: UUID,
         request: UpdateUserStatusRequest,
         current_user: Annotated[User, Depends(require_admin)],
         handler: Annotated[UpdateUserStatusHandler, Depends()],
     ) -> UserDto:
         return await handler.handle(user_id, request.status)
     ```
4. Write tests under `tests/features/`.

### Step 5: Wire into Frontends and Mobile

1. Frontend Data Access (`data/`):
   - Call the regenerated client method:
     ```typescript
     // Next.js / Angular
     import { updateUserStatus } from '@stackbraid/client-typescript';
     ```
2. Frontend Application (`application/`):
   - Expose via TanStack Query mutation (`useMutation`).
3. Mobile Data Access:
   - Call `UsersApi().updateUserStatus(...)` from `package:stackbraid_client`.

### Step 6: Verify Conformance

Run the conformance suite against the running backend to verify exact response envelope, status codes, and JSON serialization match the contract:
```bash
node contract/conformance/cli/run.mjs http://localhost:8080
```
