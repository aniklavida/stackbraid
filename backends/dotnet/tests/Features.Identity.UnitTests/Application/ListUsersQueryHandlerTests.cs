using NSubstitute;
using Shouldly;
using StackBraid.Features.Identity.Application.Queries;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.UnitTests.Application;

public class ListUsersQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ListUsersQueryHandler _sut;

    public ListUsersQueryHandlerTests()
    {
        _sut = new ListUsersQueryHandler(_users);
    }

    [Fact]
    public async Task Handle_maps_the_search_result_into_an_arithmetically_consistent_page()
    {
        var items = new[]
        {
            User.Register(Email.Create("a@example.com"), "h", "A", DateTime.UtcNow),
            User.Register(Email.Create("b@example.com"), "h", "B", DateTime.UtcNow),
        };
        _users.SearchAsync(Arg.Any<UserSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new UserSearchResult(items, TotalCount: 7));

        var result = await _sut.Handle(new ListUsersQuery(2, 2, "-createdAt", null, null, null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(2);
        result.Value.TotalItems.ShouldBe(7);
        result.Value.TotalPages.ShouldBe(4);
        result.Value.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_passes_every_filter_through_to_the_repository()
    {
        _users.SearchAsync(Arg.Any<UserSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new UserSearchResult([], 0));
        var roleId = Guid.NewGuid();

        await _sut.Handle(new ListUsersQuery(1, 20, "-createdAt", "ada", UserStatus.Active, roleId), CancellationToken.None);

        await _users.Received(1).SearchAsync(
            Arg.Is<UserSearchQuery>(q => q.Search == "ada" && q.Status == UserStatus.Active && q.RoleId == roleId),
            Arg.Any<CancellationToken>());
    }
}
