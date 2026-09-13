using Mediator;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<TokenPairDto>>;

/// <summary>
/// Exchanges credentials for a token pair. Deliberately returns the same
/// generic "invalid credentials" error whether the email is unknown, the
/// password is wrong, or the account is deactivated — telling a caller
/// which of those is true is a user-enumeration and account-status leak.
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, Result<TokenPairDto>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IAccessTokenIssuer accessTokenIssuer,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<TokenPairDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return InvalidCredentials();
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status == UserStatus.Inactive || !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return InvalidCredentials();
        }

        var now = DateTime.UtcNow;
        user.RecordLogin(now);

        var accessToken = _accessTokenIssuer.Issue(user);
        var rawRefreshToken = OpaqueTokenGenerator.GenerateRawToken();
        var refreshToken = RefreshToken.Issue(user.Id, OpaqueTokenGenerator.Hash(rawRefreshToken), now.Add(RefreshTokenLifetime), now);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TokenPairDto>.Success(new TokenPairDto(
            accessToken.Value,
            rawRefreshToken,
            TokenPairDto.BearerTokenType,
            accessToken.ExpiresAtUtc));
    }

    private static AppError InvalidCredentials() =>
        AppError.Unauthorized("IDENTITY.INVALID_CREDENTIALS", "identity.invalid_credentials");
}
