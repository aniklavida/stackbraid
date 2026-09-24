namespace StackBraid.Shared.Jobs;

/// <summary>
/// The real default <see cref="IJobScheduler"/>: <see cref="EnqueueAsync"/>
/// writes a <see cref="JobRecord"/> through <see cref="IJobStore"/> before
/// returning, so the queued work is still there after the process restarts.
/// The delegate <see cref="Enqueue"/> path stays ephemeral and is delegated
/// to an <see cref="InProcessJobScheduler"/> — the honest behaviour for
/// fire-and-forget work that never claimed durability.
/// </summary>
public sealed class PersistentJobScheduler : IJobScheduler
{
    private readonly InProcessJobScheduler _ephemeral;
    private readonly IJobStore _store;
    private readonly TimeProvider _timeProvider;

    public PersistentJobScheduler(InProcessJobScheduler ephemeral, IJobStore store, TimeProvider timeProvider)
    {
        _ephemeral = ephemeral;
        _store = store;
        _timeProvider = timeProvider;
    }

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> job) => _ephemeral.Enqueue(job);

    public async Task<Guid> EnqueueAsync(JobRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var record = new JobRecord
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            PayloadJson = request.PayloadJson,
            OwnerId = request.OwnerId,
            State = JobStates.Queued,
            Attempts = 0,
            MaxAttempts = request.MaxAttempts,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _store.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return record.Id;
    }

    public async Task<JobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var record = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        return record?.ToStatus();
    }
}
