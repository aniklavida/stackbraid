using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;

namespace StackBraid.Features.Identity.Application.Mapping;

/// <summary>
/// Hand-written mapping, deliberately not a library: a mapping library
/// hides exactly the line a reviewer needs to see when a new field is
/// added and forgotten. The cover for that one real weakness — a forgotten
/// line fails silently rather than loudly — is <c>EntityMappingExtensionsTests</c>,
/// one test per DTO asserting every property is populated from a
/// non-default source value.
/// </summary>
public static class EntityMappingExtensions
{
    public static RoleDto ToDto(this Role role) => new(
        role.Id,
        role.Name,
        role.Description,
        role.Permissions.ToList());

    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Email.Value,
        user.DisplayName,
        ToStatusString(user.Status),
        user.Roles.Select(r => r.ToDto()).ToList(),
        user.CreatedAtUtc,
        user.UpdatedAtUtc,
        user.LastLoginAtUtc);

    public static UserPageDto ToDto(this UserSearchResult result, int page, int pageSize)
    {
        var totalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize);
        return new UserPageDto(page, pageSize, result.TotalCount, totalPages, result.Items.Select(u => u.ToDto()).ToList());
    }

    private static string ToStatusString(UserStatus status) => status switch
    {
        UserStatus.Active => "active",
        UserStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown user status."),
    };
}
