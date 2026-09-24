using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Queries;

public sealed record ListUsersQuery(
    int Page,
    int PageSize,
    string Sort,
    string? Search,
    UserStatus? Status,
    Guid? RoleId,
    bool IncludeDeleted = false) : IQuery<Result<UserPageDto>>;

public sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, Result<UserPageDto>>
{
    private readonly IUserRepository _users;

    public ListUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async ValueTask<Result<UserPageDto>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await _users.SearchAsync(
            new UserSearchQuery(query.Page, query.PageSize, query.Sort, query.Search, query.Status, query.RoleId, query.IncludeDeleted),
            cancellationToken).ConfigureAwait(false);

        return Result<UserPageDto>.Success(result.ToDto(query.Page, query.PageSize));
    }
}
