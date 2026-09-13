namespace StackBraid.Features.Identity.Domain.Entities;

/// <summary>
/// The link between a <see cref="User"/> and a <see cref="Role"/>. A plain
/// join entity rather than an implicit EF many-to-many so
/// <see cref="AssignedAtUtc"/> has somewhere to live — "when was this role
/// granted" is a real audit question an admin screen will ask.
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole()
    {
    }

    internal static UserRole Create(Guid userId, Role role, DateTime assignedAtUtc) => new()
    {
        UserId = userId,
        RoleId = role.Id,
        Role = role,
        AssignedAtUtc = assignedAtUtc,
    };
}
