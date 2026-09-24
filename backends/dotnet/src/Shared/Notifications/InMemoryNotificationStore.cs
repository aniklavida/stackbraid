using System.Collections.Concurrent;
using StackBraid.Shared.Notifications;

namespace StackBraid.Shared.Notifications;

public sealed class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentDictionary<Guid, StoredNotification> _notifications = new();

    public Task AddAsync(StoredNotification notification, CancellationToken cancellationToken = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredNotification>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = _notifications.Values
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        return Task.FromResult<IReadOnlyList<StoredNotification>>(items);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_notifications.Values.Count(notification => notification.UserId == userId));
    }
}
