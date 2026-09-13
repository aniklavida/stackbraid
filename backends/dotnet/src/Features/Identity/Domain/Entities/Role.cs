using StackBraid.Features.Identity.Domain.Common;

namespace StackBraid.Features.Identity.Domain.Entities;

/// <summary>
/// A named set of permission codes. Roles are an administrator-curated
/// catalogue, not user-generated data (see <c>contract/openapi.yaml</c>'s
/// <c>/v1/roles</c> — deliberately unpaginated) — the count stays small by
/// design.
/// </summary>
public sealed class Role : Entity<Guid>, IAuditable
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IReadOnlyCollection<string> Permissions { get; private set; } = new List<string>();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    private Role()
    {
    }

    private Role(Guid id, string name, string? description, IEnumerable<string> permissions)
        : base(id)
    {
        Name = name;
        Description = description;
        Permissions = permissions.Distinct().ToList();
    }

    public static Role Create(string name, string? description, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A role must have a name.", nameof(name));
        }

        return new Role(Guid.NewGuid(), name, description, permissions);
    }

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
