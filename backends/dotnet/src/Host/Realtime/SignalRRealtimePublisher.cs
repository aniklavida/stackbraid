using Microsoft.AspNetCore.SignalR;
using StackBraid.Shared.Realtime;

namespace StackBraid.Host.Realtime;

/// <summary>
/// The real implementation of the port <c>Shared</c> declares. Sending
/// through <see cref="IHubContext{THub}"/> rather than from inside a hub
/// method is what lets an ordinary command handler (deactivate a user,
/// change a role) publish without ever depending on SignalR itself — only
/// on <see cref="IRealtimePublisher"/>. Every send goes out under the
/// method name <c>"message"</c>; a client subscribes once
/// (<c>connection.on("message", ...)</c>) and branches on the payload's own
/// <c>type</c> field, the same discriminator the contract defines.
/// </summary>
public sealed class SignalRRealtimePublisher : IRealtimePublisher
{
    private const string ClientMethod = "message";

    private readonly IHubContext<NotificationsHub> _notifications;
    private readonly IHubContext<JobsHub> _jobs;

    public SignalRRealtimePublisher(IHubContext<NotificationsHub> notifications, IHubContext<JobsHub> jobs)
    {
        _notifications = notifications;
        _jobs = jobs;
    }

    public Task PublishToUserAsync(Guid userId, RealtimeMessage message, CancellationToken cancellationToken = default) =>
        _notifications.Clients
            .Group(NotificationsHub.GroupName(userId.ToString()))
            .SendAsync(ClientMethod, message, cancellationToken);

    public Task PublishToJobAsync(Guid jobId, RealtimeMessage message, CancellationToken cancellationToken = default) =>
        _jobs.Clients
            .Group(JobsHub.GroupName(jobId))
            .SendAsync(ClientMethod, message, cancellationToken);
}
