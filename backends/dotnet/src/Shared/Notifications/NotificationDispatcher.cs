using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Jobs;

namespace StackBraid.Shared.Notifications;

public sealed class NotificationDispatcher
{
    private readonly IReadOnlyList<INotificationSender> _senders;

    public NotificationDispatcher(IEnumerable<INotificationSender> senders)
    {
        _senders = senders.ToArray();
    }

    public async Task DispatchAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        foreach (var sender in _senders)
        {
            await sender.SendAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class QueuedNotificationJob
{
    private readonly IJobScheduler _scheduler;

    public QueuedNotificationJob(IJobScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public void Enqueue(NotificationMessage notification)
    {
        _scheduler.Enqueue((services, cancellationToken) =>
            services.GetRequiredService<NotificationDispatcher>().DispatchAsync(notification, cancellationToken));
    }
}
