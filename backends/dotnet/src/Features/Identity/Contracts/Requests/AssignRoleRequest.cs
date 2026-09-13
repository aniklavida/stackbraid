namespace StackBraid.Features.Identity.Contracts.Requests;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>AssignRoleRequest</c> schema.</summary>
public sealed record AssignRoleRequest(Guid RoleId);
