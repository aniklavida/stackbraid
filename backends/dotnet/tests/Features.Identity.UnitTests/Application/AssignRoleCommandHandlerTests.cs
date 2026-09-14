using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Realtime;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class AssignRoleCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRealtimePublisher _realtime = Substitute.For<IRealtimePublisher>();
    private readonly AssignRoleCommandHandler _sut;

    public AssignRoleCommandHandlerTests()
    {
        _sut = new AssignRoleCommandHandler(_users, _roles, _unitOfWork, _realtime);
    }

    [Fact]
    public async Task Handle_assigns_the_role_and_returns_the_updated_user()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        var role = Role.Create("editor", null, []);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(new AssignRoleCommand(user.Id, role.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Roles.ShouldContain(r => r.Id == role.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _realtime.Received(1).PublishToUserAsync(
            user.Id,
            Arg.Is<RealtimeMessage>(m => m.GetType() == typeof(UserRoleChangedMessage) && ((UserRoleChangedMessage)m).Roles.Any(r => r.Id == role.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_user()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.USER_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_role()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(new AssignRoleCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.ROLE_NOT_FOUND");
    }
}
