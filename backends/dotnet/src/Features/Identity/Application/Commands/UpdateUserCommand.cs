using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record UpdateUserCommand(Guid UserId, string? DisplayName, string? Email) : ICommand<Result<UserDto>>;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return AppError.NotFound("IDENTITY.USER_NOT_FOUND", "identity.user_not_found");
        }

        Email? newEmail = null;
        if (command.Email is not null)
        {
            try
            {
                newEmail = Email.Create(command.Email);
            }
            catch (ArgumentException)
            {
                return AppError.Validation(
                    "IDENTITY.VALIDATION_FAILED",
                    "identity.validation_failed",
                    new Dictionary<string, string[]> { ["email"] = ["validation.email.invalid"] });
            }

            if (newEmail != user.Email && await _users.ExistsByEmailAsync(newEmail, cancellationToken).ConfigureAwait(false))
            {
                return AppError.Conflict("IDENTITY.EMAIL_IN_USE", "identity.email_in_use");
            }
        }

        user.UpdateProfile(command.DisplayName, newEmail);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Success(user.ToDto());
    }
}
