using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserCommandHandler _sut;

    public UpdateUserCommandHandlerTests()
    {
        _sut = new UpdateUserCommandHandler(_users, _unitOfWork);
    }

    [Fact]
    public async Task Handle_updates_only_the_display_name_when_email_is_omitted()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new UpdateUserCommand(user.Id, "Ada Lovelace", null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DisplayName.ShouldBe("Ada Lovelace");
        result.Value.Email.ShouldBe("ada@example.com");
    }

    [Fact]
    public async Task Handle_returns_conflict_when_the_new_email_is_already_in_use()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _users.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(new UpdateUserCommand(user.Id, null, "taken@example.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.EMAIL_IN_USE");
    }

    [Fact]
    public async Task Handle_allows_setting_the_email_to_its_own_current_value()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new UpdateUserCommand(user.Id, null, "ada@example.com"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _users.DidNotReceive().ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_user()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new UpdateUserCommand(Guid.NewGuid(), "New Name", null), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.USER_NOT_FOUND");
    }
}
