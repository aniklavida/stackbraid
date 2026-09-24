namespace StackBraid.Shared.Jobs;

/// <summary>
/// Where a queued job lives between being enqueued and being run — the
/// persistence port behind <see cref="JobRecord"/>. An in-memory store ships
/// for tests and single-process development; the Postgres-backed store in
/// <c>Database/Postgres</c> is the one a real deployment uses, and is why a
/// job survives an API restart.
/// </summary>
public interface IJobStore
{
    Task AddAsync(JobRecord job, CancellationToken cancellationToken = default);

    Task<JobRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims up to <paramref name="max"/> jobs that are due at
    /// <paramref name="now"/>, moving each to <see cref="JobStates.Running"/>
    /// and incrementing its attempt count, and returns them. Claiming is
    /// atomic so two workers — even two processes — never run the same job.
    /// </summary>
    Task<IReadOnlyList<JobRecord>> ClaimDueAsync(int max, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task UpdateAsync(JobRecord job, CancellationToken cancellationToken = default);
}
