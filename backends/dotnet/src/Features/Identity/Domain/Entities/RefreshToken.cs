using StackBraid.Features.Identity.Domain.Common;

namespace StackBraid.Features.Identity.Domain.Entities;

/// <summary>
/// One issued refresh token. Only its hash is ever persisted — the raw
/// value is returned to the caller once (in <c>TokenPair.refreshToken</c>)
/// and never stored, so a database read can never recover a usable token.
/// Rotation is explicit: refreshing revokes the old token and records which
/// new one replaced it, so a reused, already-rotated token is rejected
/// (see <c>contract/conformance</c>'s refreshed/expired/revoked checks).
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = nowUtc;
    }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, nowUtc);

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Revoke(DateTime nowUtc, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc ??= nowUtc;
        ReplacedByTokenId ??= replacedByTokenId;
    }
}
