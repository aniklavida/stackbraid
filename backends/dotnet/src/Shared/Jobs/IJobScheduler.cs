namespace StackBraid.Shared.Jobs;

/// <summary>
/// Queues work to run outside the request that triggered it. Two
/// implementations ship: <see cref="InProcessJobScheduler"/> — real
/// asynchronous execution, but in-process and not durable across a restart —
/// and <see cref="PersistentJobScheduler"/>, the real default, which stores
/// the job so it survives one. <see cref="Enqueue"/> remains the
/// fire-and-forget delegate path (correct for work whose loss on a restart is
/// acceptable); <see cref="EnqueueAsync"/> is the durable path.
/// </summary>
public interface IJobScheduler
{
    /// <summary>Queues an in-process delegate. Not durable; the default implementation does not persist it.</summary>
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> job);

    /// <summary>
    /// Persists a job and returns its id. A scheduler that cannot persist one
    /// refuses loudly rather than silently losing it.
    /// </summary>
    Task<Guid> EnqueueAsync(JobRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            $"This {nameof(IJobScheduler)} does not persist jobs; register {nameof(PersistentJobScheduler)} to enqueue durable work.");

    /// <summary>The owner-scoped status of a persisted job, or null if it is unknown to this scheduler.</summary>
    Task<JobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        Task.FromResult<JobStatus?>(null);
}
