using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using StackBraid.Shared.Jobs;

namespace StackBraid.Features.Identity.IntegrationTests;

/// <summary>
/// Proves the restart-survival claim against the real store rather than the
/// in-memory one: a job written by one scheduler is found by a brand-new
/// scheduler built over the same Postgres, then completed by a worker.
/// Requires the throwaway local Postgres (<c>scripts/start-local-postgres.sh</c>).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class JobStoreIntegrationTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public JobStoreIntegrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_job_persisted_in_postgres_is_found_by_a_new_scheduler_and_completes()
    {
        var store = _fixture.Services.GetRequiredService<IJobStore>();
        var time = TimeProvider.System;
        var options = Options.Create(new JobWorkerOptions { BaseRetryDelayMilliseconds = 20, MaxRetryDelayMilliseconds = 1000 });
        var handler = new RecordingJobHandler("integration.restart.job");
        var registry = new JobHandlerRegistry([handler]);

        var beforeRestart = CreateScheduler(store, time);
        var jobId = await beforeRestart.EnqueueAsync(new JobRequest(handler.JobType, "{\"value\":42}", "integration-owner", 3));

        // A new scheduler over the same Postgres — the equivalent of a fresh
        // process reading the row the previous one committed.
        var afterRestart = CreateScheduler(store, time);
        var persisted = await afterRestart.GetStatusAsync(jobId);
        persisted.ShouldNotBeNull();
        persisted!.Status.ShouldBe(JobStates.Queued);
        persisted.OwnerId.ShouldBe("integration-owner");

        var worker = new JobWorker(store, registry, options, time, NullLogger<JobWorker>.Instance);
        await worker.RunDueAsync();

        var completed = await afterRestart.GetStatusAsync(jobId);
        completed!.Status.ShouldBe(JobStates.Succeeded);
        completed.Attempts.ShouldBe(1);
        handler.Attempts.ShouldBe([1]);
    }

    private static PersistentJobScheduler CreateScheduler(IJobStore store, TimeProvider time) =>
        new(new InProcessJobScheduler(Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>()), store, time);

    private sealed class RecordingJobHandler : IJobHandler
    {
        public RecordingJobHandler(string jobType) => JobType = jobType;

        public string JobType { get; }

        public List<int> Attempts { get; } = [];

        public Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default)
        {
            Attempts.Add(context.Attempt);
            return Task.CompletedTask;
        }
    }
}
