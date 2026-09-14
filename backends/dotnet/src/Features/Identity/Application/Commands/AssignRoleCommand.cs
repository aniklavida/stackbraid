using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Realtime;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand<Result<UserDto>>;

/// <summary>No-op if the user already holds the role — <see cref="Domain.Entities.User.AssignRole"/> owns that invariant.</summary>
public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimePublisher _realtime;

    public AssignRoleCommandHandler(IUserRepository users, IRoleRepository roles, IUnitOfWork unitOfWork, IRealtimePublisher realtime)
    {
        _users = users;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    public async ValueTask<Result<UserDto>> Handle(AssignRoleCommand command, CancellationToken cancellationToken)
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

        user.AssignRole(role, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var occurredAt = DateTime.UtcNow;
        await _realtime.PublishToUserAsync(
            user.Id,
            new UserRoleChangedMessage(user.Id, user.Roles.Select(r => r.ToRealtimeSummary()).ToList(), occurredAt),
            cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Success(user.ToDto());
    }
}
