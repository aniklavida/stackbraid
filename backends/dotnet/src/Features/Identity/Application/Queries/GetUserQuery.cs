using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Queries;

public sealed record GetUserQuery(Guid UserId) : IQuery<Result<UserDto>>;

public sealed class GetUserQueryHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _users;

    public GetUserQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async ValueTask<Result<UserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(query.UserId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found")
            : Result<UserDto>.Success(user.ToDto());
    }
}
