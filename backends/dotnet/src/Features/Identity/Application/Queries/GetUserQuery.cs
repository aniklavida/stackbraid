using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Caching;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Queries;

public sealed record GetUserQuery(Guid UserId, bool IncludeDeleted = false) : IQuery<Result<UserDto>>;

public sealed class GetUserQueryHandler : IQueryHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly ICache? _cache;

    public GetUserQueryHandler(IUserRepository users, ICache? cache = null)
    {
        _users = users;
        _cache = cache;
    }

    public async ValueTask<Result<UserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        if (!query.IncludeDeleted && _cache is not null)
        {
            var cached = await _cache.GetOrCreateAsync(
                $"identity:user:{query.UserId}",
                async _ => (await _users.GetByIdAsync(query.UserId, cancellationToken).ConfigureAwait(false))?.ToDto(),
                TimeSpan.FromMinutes(5),
                cancellationToken).ConfigureAwait(false);
            return cached is null
                ? AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found")
                : Result<UserDto>.Success(cached);
        }

        var user = query.IncludeDeleted
            ? await _users.GetByIdIncludingDeletedAsync(query.UserId, cancellationToken).ConfigureAwait(false)
            : await _users.GetByIdAsync(query.UserId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found")
            : Result<UserDto>.Success(user.ToDto());
    }
}
