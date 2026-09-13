using Mediator;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;
using StackBraid.Shared.Security;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Application.Commands;

public sealed record LogoutCommand(string RawRefreshToken) : ICommand<Result>;

/// <summary>
/// Revokes one session. Idempotent by design — logging out a token that is
/// missing or already revoked is still success, per
/// <c>contract/openapi.yaml</c>'s <c>/v1/auth/logout</c>.
/// </summary>
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var hash = OpaqueTokenGenerator.Hash(command.RawRefreshToken);
        var existing = await _refreshTokens.GetByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Revoke(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
