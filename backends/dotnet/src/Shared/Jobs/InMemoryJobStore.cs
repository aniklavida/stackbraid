namespace StackBraid.Shared.Jobs;

/// <summary>
/// A thread-safe, in-memory <see cref="IJobStore"/> for tests and
/// single-process development. It holds its records in a dictionary keyed by
/// job id, so the same instance shared across two scheduler/worker
/// compositions behaves exactly like a persisted store that was not dropped —
/// which is how the restart-survival test simulates a process restart without
/// a database.
/// </summary>
public sealed class InMemoryJobStore : IJobStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, JobRecord> _jobs = [];

    public Task AddAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _jobs[job.Id] = job.Clone();
        }

        return Task.CompletedTask;
    }

    public Task<JobRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_jobs.TryGetValue(id, out var job) ? job.Clone() : null);
        }
    }

    public Task<IReadOnlyList<JobRecord>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<JobRecord> owned = _jobs.Values
                .Where(job => string.Equals(job.OwnerId, ownerId, StringComparison.Ordinal))
                .OrderBy(job => job.CreatedAt)
                .Select(job => job.Clone())
                .ToList();
            return Task.FromResult(owned);
        }
    }

    public Task<IReadOnlyList<JobRecord>> ClaimDueAsync(int max, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var claimed = _jobs.Values
                .Where(job => string.Equals(job.State, JobStates.Queued, StringComparison.Ordinal)
                    && (job.NextAttemptAt is null || job.NextAttemptAt <= now))
                .OrderBy(job => job.CreatedAt)
                .Take(max)
                .ToList();

            foreach (var job in claimed)
            {
                job.State = JobStates.Running;
                job.Attempts += 1;
                job.UpdatedAt = now;
                job.NextAttemptAt = null;
            }

            return Task.FromResult<IReadOnlyList<JobRecord>>(claimed.Select(job => job.Clone()).ToList());
        }
    }

    public Task UpdateAsync(JobRecord job, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _jobs[job.Id] = job.Clone();
        }

        return Task.CompletedTask;
    }
}
