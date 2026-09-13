namespace StackBraid.Features.Identity.Contracts.Dtos;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>Role</c> schema exactly.</summary>
public sealed record RoleDto(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions);
