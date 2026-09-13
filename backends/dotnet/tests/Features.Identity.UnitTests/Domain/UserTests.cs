using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Events;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.UnitTests.Domain;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_creates_an_active_user_and_raises_UserRegisteredEvent()
    {
        var email = Email.Create("ada@example.com");

        var user = User.Register(email, "hashed-password", "Ada Lovelace", Now);

        user.Email.ShouldBe(email);
        user.DisplayName.ShouldBe("Ada Lovelace");
        user.Status.ShouldBe(UserStatus.Active);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public void Deactivate_transitions_to_inactive_and_raises_the_event()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        user.ClearDomainEvents();

        user.Deactivate(Now);

        user.Status.ShouldBe(UserStatus.Inactive);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_twice_is_idempotent_and_raises_the_event_only_once()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        user.ClearDomainEvents();

        user.Deactivate(Now);
        user.Deactivate(Now);

        user.Status.ShouldBe(UserStatus.Inactive);
        user.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void AssignRole_adds_the_role_and_raises_the_event()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        var role = Role.Create("editor", null, ["docs:write"]);
        user.ClearDomainEvents();

        user.AssignRole(role, Now);

        user.HasRole(role.Id).ShouldBeTrue();
        user.Roles.ShouldContain(role);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRoleAssignedEvent>();
    }

    [Fact]
    public void AssignRole_twice_is_a_no_op_the_second_time()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        var role = Role.Create("editor", null, []);

        user.AssignRole(role, Now);
        user.ClearDomainEvents();
        user.AssignRole(role, Now);

        user.DomainEvents.ShouldBeEmpty();
        user.UserRoles.Count.ShouldBe(1);
    }

    [Fact]
    public void RevokeRole_removes_a_held_role_and_raises_the_event()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        var role = Role.Create("editor", null, []);
        user.AssignRole(role, Now);
        user.ClearDomainEvents();

        user.RevokeRole(role.Id, Now);

        user.HasRole(role.Id).ShouldBeFalse();
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserRoleRevokedEvent>();
    }

    [Fact]
    public void RevokeRole_the_user_never_held_is_treated_as_success_with_no_event()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        user.ClearDomainEvents();

        user.RevokeRole(Guid.NewGuid(), Now);

        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateProfile_changes_only_the_fields_supplied()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);

        user.UpdateProfile(displayName: "Ada, Countess of Lovelace", email: null);

        user.DisplayName.ShouldBe("Ada, Countess of Lovelace");
        user.Email.Value.ShouldBe("ada@example.com");
    }

    [Fact]
    public void RecordLogin_sets_LastLoginAtUtc_and_raises_the_event()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", Now);
        user.ClearDomainEvents();

        user.RecordLogin(Now);

        user.LastLoginAtUtc.ShouldBe(Now);
        user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserLoggedInEvent>();
    }
}
