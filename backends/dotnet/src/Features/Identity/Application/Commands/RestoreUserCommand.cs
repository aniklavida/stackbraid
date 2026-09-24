using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Caching;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record RestoreUserCommand(Guid UserId) : ICommand<Result<UserDto>>;

public sealed class RestoreUserCommandHandler : ICommandHandler<RestoreUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICache? _cache;

    public RestoreUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork, ICache? cache = null)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async ValueTask<Result<UserDto>> Handle(RestoreUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdIncludingDeletedAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");
        }

        user.Restore(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _cache?.Remove($"identity:user:{user.Id}");
        return Result<UserDto>.Success(user.ToDto());
    }
}
