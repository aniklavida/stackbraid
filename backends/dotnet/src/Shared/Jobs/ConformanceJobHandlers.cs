namespace StackBraid.Shared.Jobs;

/// <summary>
/// Deterministic handlers the contract conformance suite drives to prove the
/// retry and dead-letter paths, registered only when
/// <see cref="JobWorkerOptions.ConformanceEnabled"/> is on. Each is
/// deliberately stateless: its behaviour is a pure function of the attempt
/// number the worker passes in, so the outcome cannot depend on state a
/// process restart would have dropped.
/// </summary>
public sealed class ConformanceSucceedsJobHandler : IJobHandler
{
    public const string Type = "conformance.succeeds";

    public string JobType => Type;

    public Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

/// <summary>Fails until its third attempt, then succeeds — the "fails N times then succeeds" case.</summary>
public sealed class ConformanceRetriesJobHandler : IJobHandler
{
    public const string Type = "conformance.retries";
    public const int SucceedOnAttempt = 3;

    public string JobType => Type;

    public Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default)
    {
        if (context.Attempt < SucceedOnAttempt)
        {
            throw new InvalidOperationException($"conformance.retries fails on attempt {context.Attempt} and succeeds on attempt {SucceedOnAttempt}.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>Always fails — the case that must land in the dead-letter state once its attempt budget is spent.</summary>
public sealed class ConformanceDeadLetterJobHandler : IJobHandler
{
    public const string Type = "conformance.dead_letter";

    public string JobType => Type;

    public Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("conformance.dead_letter always fails.");
}
