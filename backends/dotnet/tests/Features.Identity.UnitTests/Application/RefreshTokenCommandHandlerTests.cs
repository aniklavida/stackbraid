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

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAccessTokenIssuer _accessTokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _sut = new RefreshTokenCommandHandler(_refreshTokens, _users, _accessTokenIssuer, _unitOfWork);
    }

    [Fact]
    public async Task Handle_rotates_an_active_token_and_returns_a_new_pair()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        var existing = RefreshToken.Issue(user.Id, OpaqueTokenGenerator.Hash("raw-token"), DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var expiry = DateTime.UtcNow.AddMinutes(15);
        _accessTokenIssuer.Issue(user).Returns(new IssuedAccessToken("new-jwt", expiry));

        var result = await _sut.Handle(new RefreshTokenCommand("raw-token"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.AccessToken.ShouldBe("new-jwt");
        existing.IsActive(DateTime.UtcNow).ShouldBeFalse();
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_an_unknown_token()
    {
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _sut.Handle(new RefreshTokenCommand("no-such-token"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.REFRESH_TOKEN_INVALID");
    }

    [Fact]
    public async Task Handle_rejects_an_expired_token()
    {
        var expired = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(-1));
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expired);

        var result = await _sut.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.REFRESH_TOKEN_INVALID");
    }

    [Fact]
    public async Task Handle_rejects_an_already_revoked_token()
    {
        var revoked = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        revoked.Revoke(DateTime.UtcNow);
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(revoked);

        var result = await _sut.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.REFRESH_TOKEN_INVALID");
    }

    [Fact]
    public async Task Handle_rejects_a_token_for_a_now_deactivated_user()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        user.Deactivate(DateTime.UtcNow);
        var existing = RefreshToken.Issue(user.Id, "hash", DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.REFRESH_TOKEN_INVALID");
    }
}
