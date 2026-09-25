# Playbook: Add Background Job

This playbook describes how to declare, register, and process background jobs across both .NET and Python backends in StackBraid.

## Core Rules

1. **Queue rides on the same database.** Queue and state share the same PostgreSQL, SQL Server, or MySQL database — one datastore to run and back up. External queue managers (like Redis, RabbitMQ, or commercial schedulers) are swappable adapters, not mandatory dependencies.
2. **Permissive licences only.** Hangfire was explicitly rejected because Hangfire Core carries an LGPL-3.0 licence that would bind user applications (see `docs/DEPENDENCIES.md`). StackBraid uses native, zero-dependency persistent job schedulers.
3. **Idempotent handlers with exponential backoff.** Handlers must be safe to retry on transient failures. Failed jobs back off exponentially up to a configured max retry count.

---

## Step-by-Step Procedure

### Step 1: Define the Job Payload

Job payloads must be serializable to JSON.

#### .NET (`backends/dotnet/src/Features/<Feature>/Application/Jobs/`)
```csharp
namespace StackBraid.Features.<Feature>.Application.Jobs;

public sealed record SendWelcomeEmailJob(Guid UserId, string Email, string DisplayName);
```

#### Python (`backends/python/src/app/features/<feature>/application/jobs.py`)
```python
from dataclasses import dataclass
from uuid import UUID

@dataclass
class SendWelcomeEmailJob:
    user_id: UUID
    email: str
    display_name: str
```

### Step 2: Implement the Job Handler

#### .NET
In `backends/dotnet/src/Features/<Feature>/Application/Jobs/SendWelcomeEmailJobHandler.cs`:
```csharp
using Microsoft.Extensions.Logging;
using StackBraid.Shared.Jobs;

namespace StackBraid.Features.<Feature>.Application.Jobs;

public sealed class SendWelcomeEmailJobHandler(
    ILogger<SendWelcomeEmailJobHandler> logger) : IJobHandler<SendWelcomeEmailJob>
{
    public async Task HandleAsync(SendWelcomeEmailJob job, CancellationToken ct)
    {
        logger.LogInformation("Processing welcome email job for user {UserId} ({Email})", job.UserId, job.Email);
        
        // Execute background work (e.g., render email template, send via IMailingService)
        await Task.CompletedTask;
    }
}
```

#### Python
In `backends/python/src/app/features/<feature>/application/jobs.py`:
```python
import logging
from app.shared.jobs.models import Job

logger = logging.getLogger(__name__)

class SendWelcomeEmailJobHandler:
    async def handle(self, job: Job) -> None:
        payload = job.payload
        logger.info("Processing welcome email job for user %s (%s)", payload["user_id"], payload["email"])
        # Execute background work
```

### Step 3: Register Handler with the Composition Root

#### .NET
In `backends/dotnet/src/Host/Program.cs` (or via feature service registration extensions):
```csharp
builder.Services.AddJobHandler<SendWelcomeEmailJob, SendWelcomeEmailJobHandler>("identity.send-welcome-email");
```

#### Python
In `backends/python/src/app/host/main.py` inside the lifespan startup handler:
```python
job_registry.register("identity.send-welcome-email", SendWelcomeEmailJobHandler())
```

### Step 4: Enqueue Jobs from Application Use Cases

Inject the scheduler port (`IJobScheduler` in .NET, `JobScheduler` in Python) into command handlers or domain event listeners.

#### .NET
```csharp
public sealed class RegisterUserCommandHandler(
    IJobScheduler jobScheduler,
    IUserRepository userRepository) : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        // 1. Create user in database...
        
        // 2. Enqueue background job:
        await jobScheduler.EnqueueAsync(
            "identity.send-welcome-email",
            new SendWelcomeEmailJob(user.Id, user.Email.Value, user.DisplayName),
            cancellationToken: ct);

        return user.ToDto();
    }
}
```

#### Python
```python
class RegisterUserHandler:
    def __init__(self, scheduler: JobScheduler, repo: UserRepository):
        self._scheduler = scheduler
        self._repo = repo

    async def handle(self, command: RegisterUserCommand) -> UserDto:
        # 1. Create user...
        
        # 2. Enqueue background job:
        await self._scheduler.enqueue(
            job_type="identity.send-welcome-email",
            payload={"user_id": str(user.id), "email": user.email, "display_name": user.display_name},
        )
        return user.to_dto()
```

### Step 5: Verify Job Execution and Error Handling

1. Run backend integration tests asserting job persistence and pickup:
   ```bash
   # .NET
   dotnet test backends/dotnet/tests/Features.Identity.IntegrationTests --filter FullyQualifiedName~Job
   
   # Python
   pytest backends/python/tests/integration/test_jobs.py
   ```
2. Verify that retries and dead-letter statuses behave properly if an unhandled exception is thrown during processing.
