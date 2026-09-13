using Microsoft.EntityFrameworkCore;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.Persistence.Repositories;

public sealed class RefreshTokenRepository : RepositoryBase<RefreshToken, Guid>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context)
        : base(context)
    {
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
