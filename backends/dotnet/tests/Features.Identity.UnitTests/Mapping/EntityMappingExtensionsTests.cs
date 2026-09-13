using Shouldly;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.UnitTests.Mapping;

/// <summary>
/// One test per DTO, asserting every property is populated from a
/// non-default source value — the cover for hand-written mapping's one
/// real weakness: a forgotten line fails silently rather than loudly.
/// </summary>
public class EntityMappingExtensionsTests
{
    [Fact]
    public void RoleDto_populates_every_property()
    {
        var role = Role.Create("editor", "Can edit documents.", ["docs:read", "docs:write"]);

        var dto = role.ToDto();

        dto.Id.ShouldBe(role.Id);
        dto.Id.ShouldNotBe(Guid.Empty);
        dto.Name.ShouldBe("editor");
        dto.Description.ShouldBe("Can edit documents.");
        dto.Permissions.ShouldBe(["docs:read", "docs:write"], ignoreOrder: true);
    }

    [Fact]
    public void RoleDto_maps_a_null_description_through_unchanged()
    {
        var role = Role.Create("editor", null, []);

        role.ToDto().Description.ShouldBeNull();
    }

    [Fact]
    public void UserDto_populates_every_property()
    {
        var role = Role.Create("editor", null, ["docs:read"]);
        var now = DateTime.UtcNow;
        var user = User.Register(Email.Create("ada@example.com"), "hashed-password", "Ada Lovelace", now);
        user.AssignRole(role, now);
        user.RecordLogin(now);

        var dto = user.ToDto();

        dto.Id.ShouldBe(user.Id);
        dto.Id.ShouldNotBe(Guid.Empty);
        dto.Email.ShouldBe("ada@example.com");
        dto.DisplayName.ShouldBe("Ada Lovelace");
        dto.Status.ShouldBe("active");
        dto.Roles.ShouldHaveSingleItem();
        dto.Roles[0].Id.ShouldBe(role.Id);
        dto.CreatedAt.ShouldBe(user.CreatedAtUtc);
        dto.UpdatedAt.ShouldBe(user.UpdatedAtUtc);
        dto.LastLoginAt.ShouldBe(now);
    }

    [Fact]
    public void UserDto_maps_an_inactive_status_and_a_null_last_login()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        user.Deactivate(DateTime.UtcNow);

        var dto = user.ToDto();

        dto.Status.ShouldBe("inactive");
        dto.LastLoginAt.ShouldBeNull();
    }

    [Fact]
    public void UserPageDto_populates_every_property_and_computes_total_pages()
    {
        var users = new[]
        {
            User.Register(Email.Create("a@example.com"), "h", "A", DateTime.UtcNow),
            User.Register(Email.Create("b@example.com"), "h", "B", DateTime.UtcNow),
        };
        var searchResult = new UserSearchResult(users, TotalCount: 9);

        var dto = searchResult.ToDto(page: 2, pageSize: 4);

        dto.Page.ShouldBe(2);
        dto.PageSize.ShouldBe(4);
        dto.TotalItems.ShouldBe(9);
        dto.TotalPages.ShouldBe(3);
        dto.Items.Count.ShouldBe(2);
        dto.Items[0].Email.ShouldBe("a@example.com");
    }
}
