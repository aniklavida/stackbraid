namespace StackBraid.Features.Identity.Contracts.Dtos;

/// <summary>Mirrors <c>contract/openapi.yaml</c>'s <c>User</c> schema exactly.</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    IReadOnlyList<RoleDto> Roles,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt);
