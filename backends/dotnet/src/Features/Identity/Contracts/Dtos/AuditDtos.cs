namespace StackBraid.Features.Identity.Contracts.Dtos;

public sealed record AuditEntryDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? ActorId,
    string CorrelationId,
    DateTime OccurredAt,
    string? Details);

public sealed record AuditPageDto(IReadOnlyList<AuditEntryDto> Items);
