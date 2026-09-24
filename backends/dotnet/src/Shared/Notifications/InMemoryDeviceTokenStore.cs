using System.Collections.Concurrent;

namespace StackBraid.Shared.Notifications;

public sealed class InMemoryDeviceTokenStore : IDeviceTokenStore
{
    private readonly ConcurrentDictionary<Guid, DeviceTokenRegistration> _devices = new();

    public Task<DeviceTokenRegistration> RegisterAsync(Guid userId, string token, string platform, string appVersion, CancellationToken cancellationToken = default)
    {
        var existing = _devices.Values.FirstOrDefault(device => device.Token == token);
        if (existing is not null)
        {
            throw new InvalidOperationException("The device token is already registered.");
        }

        var now = DateTime.UtcNow;
        var device = new DeviceTokenRegistration(Guid.NewGuid(), userId, token, platform, appVersion, now, now);
        _devices[device.Id] = device;
        return Task.FromResult(device);
    }

    public Task<DeviceTokenRegistration?> RefreshAsync(Guid userId, Guid deviceId, string token, CancellationToken cancellationToken = default)
    {
        if (!_devices.TryGetValue(deviceId, out var device) || device.UserId != userId)
        {
            return Task.FromResult<DeviceTokenRegistration?>(null);
        }

        var refreshed = device with { Token = token, UpdatedAtUtc = DateTime.UtcNow };
        _devices[deviceId] = refreshed;
        return Task.FromResult<DeviceTokenRegistration?>(refreshed);
    }

    public Task<bool> RevokeAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        if (_devices.TryGetValue(deviceId, out var device) && device.UserId == userId)
        {
            return Task.FromResult(_devices.TryRemove(deviceId, out _));
        }

        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<string>> GetTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> tokens = _devices.Values
            .Where(device => device.UserId == userId)
            .Select(device => device.Token)
            .ToArray();
        return Task.FromResult(tokens);
    }
}
