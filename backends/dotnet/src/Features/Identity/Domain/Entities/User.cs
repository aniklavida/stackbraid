using StackBraid.Features.Identity.Domain.Common;
using StackBraid.Features.Identity.Domain.Events;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.Domain.Entities;

/// <summary>
/// An account. Owns its own invariants — a caller cannot put a
/// <see cref="User"/> into a state the contract forbids (assigning a role
/// twice, deactivating twice) because the methods below are the only way to
/// change one.
/// </summary>
public sealed class User : DomainEventEntity<Guid>, IAuditable
{
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<Role> Roles => _userRoles.Select(ur => ur.Role).ToList().AsReadOnly();

    private User()
    {
    }

    private User(Guid id, Email email, string passwordHash, string displayName)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Status = UserStatus.Active;
    }

    public static User Register(Email email, string passwordHash, string displayName, DateTime nowUtc)
    {
        var user = new User(Guid.NewGuid(), email, passwordHash, displayName);
        user.Raise(new UserRegisteredEvent(user.Id, email.Value, nowUtc));
        return user;
    }

    public void RecordLogin(DateTime nowUtc)
    {
        LastLoginAtUtc = nowUtc;
        Raise(new UserLoggedInEvent(Id, nowUtc));
    }

    /// <summary>Partial update — only the fields the caller actually supplied change (see <c>UpdateUserRequest</c> in the contract).</summary>
    public void UpdateProfile(string? displayName, Email? email)
    {
        if (displayName is not null)
        {
            DisplayName = displayName;
        }

        if (email is not null)
        {
            Email = email;
        }
    }

    /// <summary>Idempotent — deactivating an already-inactive user is a no-op, per the contract's <c>/v1/users/{userId}/deactivate</c>.</summary>
    public void Deactivate(DateTime nowUtc)
    {
        if (Status == UserStatus.Inactive)
        {
            return;
        }

        Status = UserStatus.Inactive;
        DeletedAtUtc = nowUtc;
        Raise(new UserDeactivatedEvent(Id, nowUtc));
    }

    public void Restore(DateTime nowUtc)
    {
        if (DeletedAtUtc is null)
        {
            return;
        }

        DeletedAtUtc = null;
        Status = UserStatus.Active;
    }

    public bool HasRole(Guid roleId) => _userRoles.Any(ur => ur.RoleId == roleId);

    /// <summary>No-op if the user already holds this role — per the contract, assigning a held role returns the same state rather than erroring.</summary>
    public void AssignRole(Role role, DateTime nowUtc)
    {
        if (HasRole(role.Id))
        {
            return;
        }

        _userRoles.Add(UserRole.Create(Id, role, nowUtc));
        Raise(new UserRoleAssignedEvent(Id, role.Id, nowUtc));
    }

    /// <summary>Revoking a role the user does not hold is treated as success — per the contract's <c>DELETE /v1/users/{userId}/roles/{roleId}</c>.</summary>
    public void RevokeRole(Guid roleId, DateTime nowUtc)
    {
        var link = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (link is null)
        {
            return;
        }

        _userRoles.Remove(link);
        Raise(new UserRoleRevokedEvent(Id, roleId, nowUtc));
    }
}
