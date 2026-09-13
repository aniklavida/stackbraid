using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests()
    {
        _sut = new LogoutCommandHandler(_refreshTokens, _unitOfWork);
    }

    [Fact]
    public async Task Handle_revokes_an_existing_token_and_saves()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), OpaqueTokenGenerator.Hash("raw"), DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var result = await _sut.Handle(new LogoutCommand("raw"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        token.IsActive(DateTime.UtcNow).ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_is_a_success_even_when_the_token_does_not_exist()
    {
        _refreshTokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _sut.Handle(new LogoutCommand("no-such-token"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
