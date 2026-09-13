using StackBraid.Features.Identity.Domain.Common;

namespace StackBraid.Features.Identity.Domain.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record UserDeactivatedEvent(Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record UserRoleAssignedEvent(Guid UserId, Guid RoleId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record UserRoleRevokedEvent(Guid UserId, Guid RoleId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record UserLoggedInEvent(Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;
