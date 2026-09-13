using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class RevokeRoleCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RevokeRoleCommandHandler _sut;

    public RevokeRoleCommandHandlerTests()
    {
        _sut = new RevokeRoleCommandHandler(_users, _roles, _unitOfWork);
    }

    [Fact]
    public async Task Handle_revokes_a_held_role()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        var role = Role.Create("editor", null, []);
        user.AssignRole(role, DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(new RevokeRoleCommand(user.Id, role.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        user.HasRole(role.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_succeeds_even_when_the_user_never_held_the_role()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        var role = Role.Create("editor", null, []);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(new RevokeRoleCommand(user.Id, role.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_user()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new RevokeRoleCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.USER_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_role()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(new RevokeRoleCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.ROLE_NOT_FOUND");
    }
}
