using Mediator;
using StackBraid.Features.Identity.Application.Mapping;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName) : ICommand<Result<UserDto>>;

/// <summary>
/// One handler, one operation, no jump to another project to see what it
/// does: validates the request, hashes the password, creates the account,
/// and returns it — <see cref="User.Register"/> owns the actual domain
/// invariant (an active account, one <c>UserRegisteredEvent</c>).
/// </summary>
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegisterUserCommandValidator _validator = new();

    public RegisterUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<UserDto>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return validation.ToAppError("IDENTITY.VALIDATION_FAILED", "identity.validation_failed");
        }

        var email = Email.Create(command.Email);

        if (await _users.ExistsByEmailAsync(email, cancellationToken).ConfigureAwait(false))
        {
            return AppError.Conflict("IDENTITY.EMAIL_ALREADY_REGISTERED", "identity.email_already_registered");
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Register(email, passwordHash, command.DisplayName, DateTime.UtcNow);

        await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UserDto>.Success(user.ToDto());
    }
}
