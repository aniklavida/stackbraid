using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Queries;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class GetUserQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly GetUserQueryHandler _sut;

    public GetUserQueryHandlerTests()
    {
        _sut = new GetUserQueryHandler(_users);
    }

    [Fact]
    public async Task Handle_returns_the_user_when_found()
    {
        var user = User.Register(Email.Create("ada@example.com"), "hashed", "Ada", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new GetUserQuery(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_id()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(new GetUserQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("IDENTITY.USER_NOT_FOUND");
    }
}
