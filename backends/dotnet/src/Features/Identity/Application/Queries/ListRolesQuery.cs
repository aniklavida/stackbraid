using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Queries;

public sealed record ListRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;

public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IRoleRepository _roles;

    public ListRolesQueryHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async ValueTask<Result<IReadOnlyList<RoleDto>>> Handle(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await _roles.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<RoleDto>>.Success(roles.Select(r => r.ToDto()).ToList());
    }
}
