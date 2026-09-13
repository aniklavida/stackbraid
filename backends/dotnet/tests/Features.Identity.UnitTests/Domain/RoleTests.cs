using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;

namespace StackBraid.Features.Identity.UnitTests.Domain;

public class RoleTests
{
    [Fact]
    public void Create_deduplicates_permissions()
    {
        var role = Role.Create("admin", null, ["users:read", "users:read", "users:write"]);

        role.Permissions.Count.ShouldBe(2);
    }

    [Fact]
    public void HasPermission_is_true_only_for_a_granted_permission()
    {
        var role = Role.Create("admin", null, ["users:read"]);

        role.HasPermission("users:read").ShouldBeTrue();
        role.HasPermission("users:write").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        Should.Throw<ArgumentException>(() => Role.Create(name, null, []));
    }
}
