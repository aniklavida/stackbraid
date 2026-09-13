using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterUserCommandHandler _sut;

    public RegisterUserCommandHandlerTests()
    {
        _sut = new RegisterUserCommandHandler(_users, _passwordHasher, _unitOfWork);
    }

    [Fact]
    public async Task Handle_registers_a_new_user_with_a_hashed_password()
    {
        _users.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("s3cret!!").Returns("hashed-value");

        var result = await _sut.Handle(new RegisterUserCommand("ada@example.com", "s3cret!!", "Ada"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Email.ShouldBe("ada@example.com");
        result.Value.DisplayName.ShouldBe("Ada");
        result.Value.Status.ShouldBe("active");
        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.PasswordHash == "hashed-value"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_conflict_when_the_email_is_already_registered()
    {
        _users.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(new RegisterUserCommand("ada@example.com", "s3cret!!", "Ada"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(AppErrorType.Conflict);
        result.Error.Code.ShouldBe("IDENTITY.EMAIL_ALREADY_REGISTERED");
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_validation_error_for_an_invalid_email()
    {
        var result = await _sut.Handle(new RegisterUserCommand("not-an-email", "s3cret!!", "Ada"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Type.ShouldBe(AppErrorType.Validation);
        result.Error.FieldErrors!.ShouldContainKey("email");
    }
}
