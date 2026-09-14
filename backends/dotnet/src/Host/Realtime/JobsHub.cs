using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Realtime;

namespace StackBraid.Host.Realtime;

/// <summary>
/// <c>/v1/hubs/jobs</c> — carries <c>JobProgressMessage</c>, group
/// <c>job:{jobId}</c>, per <c>contract/openapi.yaml</c>'s
/// <c>x-realtime-channels</c> section. A job id is chosen by whichever
/// client starts the job (a UUID it generates itself), so — unlike
/// <see cref="NotificationsHub"/>, where the group is derived from the
/// authenticated identity — the client tells this hub which job it wants
/// to watch after connecting.
/// </summary>
[Authorize]
public sealed class JobsHub : Hub
{
    private readonly IJobScheduler _jobs;

    public JobsHub(IJobScheduler jobs)
    {
        _jobs = jobs;
    }

    public Task Subscribe(Guid jobId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(jobId));

    /// <summary>
    /// Starts a simulated job that reports progress at four points —
    /// enough to prove a client watching <paramref name="jobId"/> from a
    /// different connection (a second browser tab, or a second backend
    /// instance behind the same backplane) receives every frame. Nothing
    /// here performs real work; that is deliberate; see
    /// <c>docs/SPEC.md</c> §13 — the shipped realtime surface exists to
    /// prove the plumbing, not to be a job of its own.
    /// </summary>
    public Task StartDemoJob(Guid jobId)
    {
        _jobs.Enqueue(async (services, cancellationToken) =>
        {
            var realtime = services.GetRequiredService<IRealtimePublisher>();
            foreach (var (status, progress) in Frames)
            {
                await realtime.PublishToJobAsync(
                    jobId,
                    new JobProgressMessage(jobId, status, progress, DateTime.UtcNow),
                    cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            }
        });

        return Task.CompletedTask;
    }

    public static string GroupName(Guid jobId) => $"job:{jobId}";

    private static readonly (string Status, int Progress)[] Frames =
    [
        ("queued", 0),
        ("running", 40),
        ("running", 80),
        ("succeeded", 100),
    ];
}
