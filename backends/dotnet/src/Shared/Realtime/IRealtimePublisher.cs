namespace StackBraid.Shared.Realtime;

/// <summary>
/// Pushes a <see cref="RealtimeMessage"/> to whichever clients are
/// subscribed to a user's notification stream or a job's progress channel.
/// One implementation ships today, <c>SignalRRealtimePublisher</c>
/// (<c>Host/Realtime</c>) — it always delivers to clients connected to
/// *this* process; a Redis (or Valkey — both speak the same protocol)
/// backplane is layered on underneath via SignalR's own
/// <c>AddStackExchangeRedis</c> only when a connection string is
/// configured, so a caller here never knows or cares how many instances
/// are running. Nothing in this interface, or in <c>Shared</c> generally,
/// names Redis or any other backplane technology.
/// </summary>
public interface IRealtimePublisher
{
    Task PublishToUserAsync(Guid userId, RealtimeMessage message, CancellationToken cancellationToken = default);

    Task PublishToJobAsync(Guid jobId, RealtimeMessage message, CancellationToken cancellationToken = default);
}
