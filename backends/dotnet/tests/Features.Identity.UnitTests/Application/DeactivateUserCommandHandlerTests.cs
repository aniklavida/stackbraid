using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Realtime;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class DeactivateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRealtimePublisher _realtime = Substitute.For<IRealtimePublisher>();
    private readonly DeactivateUserCommandHandler _sut;

    public DeactivateUserCommandHandlerTests()
    {
        _sut = new DeactivateUserCommandHandler(_users, _unitOfWork, _realtime);
    }

    [Fact]
    public async Task Handle_deactivates_an_active_user()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("inactive");
        await _realtime.Received(1).PublishToUserAsync(
            user.Id,
            Arg.Is<RealtimeMessage>(m => m.GetType() == typeof(UserDeactivatedMessage)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_is_idempotent_for_an_already_inactive_user_and_does_not_notify_again()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        user.Deactivate(DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("inactive");
        await _realtime.DidNotReceive().PublishToUserAsync(
            Arg.Any<Guid>(),
            Arg.Any<RealtimeMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_user()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.USER_NOT_FOUND");
    }
}
