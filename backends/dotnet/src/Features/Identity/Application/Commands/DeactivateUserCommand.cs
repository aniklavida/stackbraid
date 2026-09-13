using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand<Result<UserDto>>;

/// <summary>Idempotent — deactivating an already-inactive user returns the current state, not an error.</summary>
public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<UserDto>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");
        }

        user.Deactivate(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Success(user.ToDto());
    }
}
