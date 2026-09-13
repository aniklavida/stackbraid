using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_users, _refreshTokens, _passwordHasher, _accessTokenIssuer, _unitOfWork);
    }

    private static User ActiveUser() => User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);

    [Fact]
    public async Task Handle_returns_a_token_pair_for_correct_credentials()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", "hashed").Returns(true);
        var expiry = DateTime.UtcNow.AddMinutes(15);
        _accessTokenIssuer.Issue(user).Returns(new IssuedAccessToken("jwt-value", expiry));

        var result = await _sut.Handle(new LoginCommand("ada@example.com", "correct"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.AccessToken.ShouldBe("jwt-value");
        result.Value.TokenType.ShouldBe("Bearer");
        result.Value.ExpiresAt.ShouldBe(expiry);
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_an_unknown_email_with_a_generic_error()
    {
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_rejects_a_wrong_password_with_the_same_generic_error()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "hashed").Returns(false);

        var result = await _sut.Handle(new LoginCommand("ada@example.com", "wrong"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_rejects_a_deactivated_user_with_the_same_generic_error()
    {
        var user = ActiveUser();
        user.Deactivate(DateTime.UtcNow);
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", "hashed").Returns(true);

        var result = await _sut.Handle(new LoginCommand("ada@example.com", "correct"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_records_the_login_time_on_the_user()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", "hashed").Returns(true);
        _accessTokenIssuer.Issue(user).Returns(new IssuedAccessToken("jwt", DateTime.UtcNow.AddMinutes(15)));

        await _sut.Handle(new LoginCommand("ada@example.com", "correct"), CancellationToken.None);

        user.LastLoginAtUtc.ShouldNotBeNull();
    }
}
