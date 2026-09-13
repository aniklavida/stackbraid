using Mediator;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record RefreshTokenCommand(string RawRefreshToken) : ICommand<Result<TokenPairDto>>;

/// <summary>
/// Rotates a refresh token: the presented token is revoked (recording which
/// token replaced it) and a new pair is issued. A missing, expired, or
/// already-rotated token all produce the same 401 — see
/// <c>contract/openapi.yaml</c>'s <c>/v1/auth/refresh</c>.
/// </summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<TokenPairDto>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IAccessTokenIssuer accessTokenIssuer,
        IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _accessTokenIssuer = accessTokenIssuer;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<TokenPairDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var presentedHash = OpaqueTokenGenerator.Hash(command.RawRefreshToken);
        var existing = await _refreshTokens.GetByTokenHashAsync(presentedHash, cancellationToken).ConfigureAwait(false);

        if (existing is null || !existing.IsActive(now))
        {
            return AppError.Unauthorized("IDENTITY.REFRESH_TOKEN_INVALID", "identity.refresh_token_invalid");
        }

        var user = await _users.GetByIdAsync(existing.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status == UserStatus.Inactive)
        {
            return AppError.Unauthorized("IDENTITY.REFRESH_TOKEN_INVALID", "identity.refresh_token_invalid");
        }

        var rawNewToken = OpaqueTokenGenerator.GenerateRawToken();
        var newToken = RefreshToken.Issue(user.Id, OpaqueTokenGenerator.Hash(rawNewToken), now.Add(RefreshTokenLifetime), now);
        existing.Revoke(now, newToken.Id);

        await _refreshTokens.AddAsync(newToken, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var accessToken = _accessTokenIssuer.Issue(user);

        return Result<TokenPairDto>.Success(new TokenPairDto(
            accessToken.Value,
            rawNewToken,
            TokenPairDto.BearerTokenType,
            accessToken.ExpiresAtUtc));
    }
}
