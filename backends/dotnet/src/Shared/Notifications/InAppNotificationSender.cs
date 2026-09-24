namespace StackBraid.Shared.Notifications;

public sealed class InAppNotificationSender : INotificationSender
{
    private readonly INotificationStore _store;

    public InAppNotificationSender(INotificationStore store)
    {
        _store = store;
    }

    public string Channel => "in-app";

    public Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        return _store.AddAsync(
            new StoredNotification(Guid.NewGuid(), notification.UserId, notification.Title, notification.Body, false, DateTime.UtcNow),
            cancellationToken);
    }
}
