using Microsoft.EntityFrameworkCore;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.Persistence.Repositories;

public sealed class RoleRepository : RepositoryBase<Role, Guid>, IRoleRepository
{
    public RoleRepository(IdentityDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.OrderBy(r => r.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
}
