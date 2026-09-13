using Mediator;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record RevokeRoleCommand(Guid UserId, Guid RoleId) : ICommand<Result>;

/// <summary>
/// Revoking a role the user does not hold is still success — see
/// <c>contract/openapi.yaml</c>'s <c>DELETE /v1/users/{userId}/roles/{roleId}</c>.
/// The user or the role not existing at all is the only 404 case.
/// </summary>
public sealed class RevokeRoleCommandHandler : ICommandHandler<RevokeRoleCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRoleCommandHandler(IUserRepository users, IRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _users = users;
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(RevokeRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");
        }

        var role = await _roles.GetByIdAsync(command.RoleId, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return AppError.NotFound("IDENTITY.ROLE_NOT_FOUND", "identity.role_not_found");
        }

        user.RevokeRole(command.RoleId, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
