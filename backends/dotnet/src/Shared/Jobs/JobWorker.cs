using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace StackBraid.Shared.Jobs;

/// <summary>
/// Drains persisted jobs from an <see cref="IJobStore"/>: claim the due ones,
/// run each through its <see cref="IJobHandler"/>, and record the outcome. A
/// failure is retried with exponential backoff until the job's attempt budget
/// is spent, at which point it lands in the dead-letter state with the last
/// error preserved. <see cref="RunDueAsync"/> is exposed so a test can drive
/// a single pass deterministically instead of waiting on the poll loop.
/// </summary>
public sealed class JobWorker
{
    private readonly IJobStore _store;
    private readonly IJobHandlerRegistry _registry;
    private readonly JobWorkerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(
        IJobStore store,
        IJobHandlerRegistry registry,
        IOptions<JobWorkerOptions> options,
        TimeProvider timeProvider,
        ILogger<JobWorker> logger)
    {
        _store = store;
        _registry = registry;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RunDueAsync(CancellationToken cancellationToken = default)
    {
        var due = await _store.ClaimDueAsync(_options.BatchSize, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        foreach (var job in due)
        {
            await ExecuteAsync(job, cancellationToken).ConfigureAwait(false);
        }

        return due.Count;
    }

    private async Task ExecuteAsync(JobRecord job, CancellationToken cancellationToken)
    {
        // The job's own span joins the trace that enqueued it (the persisted
        // traceparent is its remote parent) and both it and every log line
        // emitted below carry the correlation ID captured at enqueue time.
        using var activity = JobTelemetry.StartAttempt(job);
        using var correlationScope = job.CorrelationId is null ? null : LogContext.PushProperty("CorrelationId", job.CorrelationId);
        using var traceScope = activity is null ? null : LogContext.PushProperty("TraceId", activity.TraceId.ToString());
        using var spanScope = activity is null ? null : LogContext.PushProperty("SpanId", activity.SpanId.ToString());

        var startedAt = _timeProvider.GetTimestamp();
        var handler = _registry.Resolve(job.Type);
        if (handler is null)
        {
            var outcome = await FailAsync(job, $"No handler is registered for job type '{job.Type}'.", JobTelemetry.NoHandlerReason, cancellationToken).ConfigureAwait(false);
            JobTelemetry.RecordDuration(job, outcome, _timeProvider.GetElapsedTime(startedAt).TotalSeconds);
            return;
        }

        var context = new JobContext(job.Id, job.Type, job.OwnerId, job.Attempts);
        try
        {
            await handler.HandleAsync(context, new JobPayload(job.PayloadJson), cancellationToken).ConfigureAwait(false);
            job.State = JobStates.Succeeded;
            job.LastError = null;
            job.NextAttemptAt = null;
            job.UpdatedAt = _timeProvider.GetUtcNow();
            await _store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            JobTelemetry.RecordDuration(job, JobTelemetry.SucceededOutcome, _timeProvider.GetElapsedTime(startedAt).TotalSeconds);
            _logger.LogInformation("Job {JobId} ({JobType}) succeeded on attempt {Attempt}.", job.Id, job.Type, job.Attempts);
        }
        catch (Exception ex)
        {
            var outcome = await FailAsync(job, ex.Message, JobTelemetry.HandlerErrorReason, cancellationToken).ConfigureAwait(false);
            JobTelemetry.RecordDuration(job, outcome, _timeProvider.GetElapsedTime(startedAt).TotalSeconds);
        }
    }

    private async Task<string> FailAsync(JobRecord job, string error, string reason, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        job.LastError = error;
        job.UpdatedAt = now;
        JobTelemetry.RecordFailure(job, reason);

        if (job.Attempts >= job.MaxAttempts)
        {
            job.State = JobStates.DeadLettered;
            job.NextAttemptAt = null;
            await _store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            _logger.LogError("Job {JobId} ({JobType}) exhausted its {Attempts} attempt(s) and was dead-lettered: {Error}", job.Id, job.Type, job.Attempts, error);
            return JobTelemetry.DeadLetteredOutcome;
        }

        var delay = BackoffPolicy.Compute(job.Attempts, _options);
        job.State = JobStates.Queued;
        job.NextAttemptAt = now + delay;
        await _store.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("Job {JobId} ({JobType}) failed on attempt {Attempt}; retrying in {Delay}: {Error}", job.Id, job.Type, job.Attempts, delay, error);
        return JobTelemetry.RetryingOutcome;
    }
}

/// <summary>Runs <see cref="JobWorker"/> on the poll interval, in whichever process registered it — the API, or a dedicated worker process.</summary>
public sealed class JobWorkerHostedService : BackgroundService
{
    private readonly JobWorker _worker;
    private readonly JobWorkerOptions _options;
    private readonly ILogger<JobWorkerHostedService> _logger;

    public JobWorkerHostedService(JobWorker worker, IOptions<JobWorkerOptions> options, ILogger<JobWorkerHostedService> logger)
    {
        _worker = worker;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WorkerEnabled)
        {
            _logger.LogInformation("Job worker is disabled in this process (Jobs:WorkerEnabled=false); a dedicated worker is expected to drain the queue.");
            return;
        }

        var poll = TimeSpan.FromMilliseconds(_options.PollIntervalMilliseconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _worker.RunDueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The job worker pass failed; it will retry on the next poll.");
            }

            try
            {
                await Task.Delay(poll, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
