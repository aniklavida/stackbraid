using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using StackBraid.Shared.Jobs;

namespace StackBraid.Shared.UnitTests.Jobs;

public sealed class JobSchedulerTests
{
    [Fact]
    public async Task Queued_job_survives_scheduler_recreation_and_completes()
    {
        // One store instance stands in for the persisted job store. Building a
        // second scheduler/worker over it — and dropping the first — is the
        // process restart: if the job lived only in the first scheduler's
        // memory it would be gone.
        var store = new InMemoryJobStore();
        var time = new MutableTimeProvider();
        var options = Options.Create(new JobWorkerOptions { BaseRetryDelayMilliseconds = 50, MaxRetryDelayMilliseconds = 1000 });
        var handler = new RecordingJobHandler("restart.job");
        var registry = new JobHandlerRegistry([handler]);

        var beforeRestart = CreateScheduler(store, time);
        var jobId = await beforeRestart.EnqueueAsync(new JobRequest("restart.job", "{}", "owner-1", 3));

        // The first scheduler is now gone; the new one must find the job purely
        // through the shared store.
        var afterRestart = CreateScheduler(store, time);
        var persisted = await afterRestart.GetStatusAsync(jobId);
        persisted.ShouldNotBeNull();
        persisted!.Status.ShouldBe(JobStates.Queued);
        persisted.OwnerId.ShouldBe("owner-1");

        var worker = new JobWorker(store, registry, options, time, NullLogger<JobWorker>.Instance);
        await worker.RunDueAsync();

        var completed = await afterRestart.GetStatusAsync(jobId);
        completed.ShouldNotBeNull();
        completed!.Status.ShouldBe(JobStates.Succeeded);
        handler.Attempts.ShouldBe([1]);
    }

    [Fact]
    public async Task Failed_job_retries_with_exponential_backoff_then_succeeds()
    {
        var store = new InMemoryJobStore();
        var time = new MutableTimeProvider();
        var options = Options.Create(new JobWorkerOptions { BaseRetryDelayMilliseconds = 50, MaxRetryDelayMilliseconds = 5000 });
        var handler = new RecordingJobHandler(ConformanceRetriesJobHandler.Type, context =>
            context.Attempt < ConformanceRetriesJobHandler.SucceedOnAttempt
                ? Task.FromException(new InvalidOperationException("not yet"))
                : Task.CompletedTask);
        var registry = new JobHandlerRegistry([handler]);
        var worker = new JobWorker(store, registry, options, time, NullLogger<JobWorker>.Instance);
        var scheduler = CreateScheduler(store, time);

        var jobId = await scheduler.EnqueueAsync(new JobRequest(handler.JobType, "{}", "owner-2", 5));
        var start = time.GetUtcNow();

        await worker.RunDueAsync();
        var afterFirstFailure = await scheduler.GetStatusAsync(jobId);
        afterFirstFailure!.Attempts.ShouldBe(1);
        afterFirstFailure.Status.ShouldBe(JobStates.Queued);
        afterFirstFailure.UpdatedAt.ShouldBe(start);

        // Not due yet — a pass before the backoff elapses must claim nothing.
        await worker.RunDueAsync();
        (await scheduler.GetStatusAsync(jobId))!.Attempts.ShouldBe(1);

        time.Advance(TimeSpan.FromMilliseconds(50));
        await worker.RunDueAsync();
        var afterSecondFailure = await scheduler.GetStatusAsync(jobId);
        afterSecondFailure!.Attempts.ShouldBe(2);
        afterSecondFailure.UpdatedAt.ShouldBe(start + TimeSpan.FromMilliseconds(50));

        time.Advance(TimeSpan.FromMilliseconds(100));
        await worker.RunDueAsync();
        var completed = await scheduler.GetStatusAsync(jobId);
        completed!.Status.ShouldBe(JobStates.Succeeded);
        completed.Attempts.ShouldBe(3);
        handler.Attempts.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Job_that_always_fails_lands_in_dead_letter_after_its_attempt_budget()
    {
        var store = new InMemoryJobStore();
        var time = new MutableTimeProvider();
        var options = Options.Create(new JobWorkerOptions { BaseRetryDelayMilliseconds = 25, MaxRetryDelayMilliseconds = 1000 });
        var handler = new RecordingJobHandler(ConformanceDeadLetterJobHandler.Type, _ =>
            Task.FromException(new InvalidOperationException("always fails")));
        var registry = new JobHandlerRegistry([handler]);
        var worker = new JobWorker(store, registry, options, time, NullLogger<JobWorker>.Instance);
        var scheduler = CreateScheduler(store, time);

        var jobId = await scheduler.EnqueueAsync(new JobRequest(handler.JobType, "{}", "owner-3", 2));

        await worker.RunDueAsync();
        (await scheduler.GetStatusAsync(jobId))!.Status.ShouldBe(JobStates.Queued);

        time.Advance(TimeSpan.FromMilliseconds(25));
        await worker.RunDueAsync();

        var deadLettered = await scheduler.GetStatusAsync(jobId);
        deadLettered!.Status.ShouldBe(JobStates.DeadLettered);
        deadLettered.Attempts.ShouldBe(2);
        deadLettered.LastError.ShouldNotBeNull().ShouldContain("always fails");
        handler.Attempts.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task In_process_scheduler_refuses_to_persist_a_job()
    {
        IJobScheduler scheduler = new InProcessJobScheduler(Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>());

        await Should.ThrowAsync<NotSupportedException>(() => scheduler.EnqueueAsync(new JobRequest("noop")));
    }

    private static PersistentJobScheduler CreateScheduler(IJobStore store, TimeProvider time) =>
        new(new InProcessJobScheduler(Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>()), store, time);

    private sealed class RecordingJobHandler : IJobHandler
    {
        private readonly Func<JobContext, Task> _behavior;

        public RecordingJobHandler(string jobType, Func<JobContext, Task>? behavior = null)
        {
            JobType = jobType;
            _behavior = behavior ?? (_ => Task.CompletedTask);
        }

        public string JobType { get; }

        public List<int> Attempts { get; } = [];

        public Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default)
        {
            Attempts.Add(context.Attempt);
            return _behavior(context);
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
