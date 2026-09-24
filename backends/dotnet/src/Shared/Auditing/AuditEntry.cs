namespace StackBraid.Shared.Auditing;

public sealed record AuditEntry(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? ActorId,
    string CorrelationId,
    DateTime OccurredAtUtc,
    string? Details);

public interface IAuditLog
{
    void Add(AuditEntry entry);

    Task<IReadOnlyList<AuditEntry>> ListAsync(Guid entityId, CancellationToken cancellationToken = default);
}
