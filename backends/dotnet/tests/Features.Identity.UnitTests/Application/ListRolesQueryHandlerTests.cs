using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Queries;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class ListRolesQueryHandlerTests
{
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ListRolesQueryHandler _sut;

    public ListRolesQueryHandlerTests()
    {
        _sut = new ListRolesQueryHandler(_roles);
    }

    [Fact]
    public async Task Handle_returns_every_role_mapped_to_a_dto()
    {
        var roles = new[] { Role.Create("admin", null, ["users:read"]), Role.Create("user", null, []) };
        _roles.ListAllAsync(Arg.Any<CancellationToken>()).Returns(roles);

        var result = await _sut.Handle(new ListRolesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(r => r.Name).ShouldBe(["admin", "user"]);
    }
}
