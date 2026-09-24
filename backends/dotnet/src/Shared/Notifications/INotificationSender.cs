namespace StackBraid.Shared.Notifications;

public sealed record NotificationMessage(
    Guid UserId,
    string Email,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data = null);

public interface INotificationSender
{
    string Channel { get; }

    Task SendAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
}

public sealed record StoredNotification(
    Guid Id,
    Guid UserId,
    string Title,
    string Body,
    bool Read,
    DateTime CreatedAtUtc);

public interface INotificationStore
{
    Task AddAsync(StoredNotification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredNotification>> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record DeviceTokenRegistration(
    Guid Id,
    Guid UserId,
    string Token,
    string Platform,
    string AppVersion,
    DateTime RegisteredAtUtc,
    DateTime UpdatedAtUtc);

public interface IDeviceTokenStore
{
    Task<DeviceTokenRegistration> RegisterAsync(Guid userId, string token, string platform, string appVersion, CancellationToken cancellationToken = default);

    Task<DeviceTokenRegistration?> RefreshAsync(Guid userId, Guid deviceId, string token, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
