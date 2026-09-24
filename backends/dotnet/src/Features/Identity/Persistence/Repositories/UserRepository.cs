using Microsoft.EntityFrameworkCore;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.Persistence.Repositories;

public sealed class UserRepository : RepositoryBase<User, Guid>, IUserRepository
{
    public UserRepository(IdentityDbContext context)
        : base(context)
    {
    }

    private IQueryable<User> WithRoles => Set.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    public override Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithRoles.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithRoles.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        WithRoles.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<UserSearchResult> SearchAsync(UserSearchQuery query, CancellationToken cancellationToken = default)
    {
        var q = WithRoles.AsQueryable();
        if (query.IncludeDeleted)
        {
            q = q.IgnoreQueryFilters();
        }

        if (query.Status is not null)
        {
            q = q.Where(u => u.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // LIKE rather than a provider's own case-insensitive operator
            // (e.g. Postgres's ILIKE) — this repository must stay
            // relational-provider-agnostic, per docs/STRUCTURE.md. Email is
            // always stored lower-cased (Email.Create normalizes it), so
            // lower-casing only the search term is enough to match it
            // case-insensitively; DisplayName has no such guarantee, so it
            // is lower-cased on both sides.
            var term = query.Search.ToLowerInvariant();
            q = q.Where(u =>
                u.DisplayName.ToLower().Contains(term) ||
                EF.Functions.Like(u.Email, $"%{term}%"));
        }

        if (query.RoleId is not null)
        {
            q = q.Where(u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId));
        }

        q = ApplySort(q, query.Sort);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UserSearchResult(items, totalCount);
    }

    private static IQueryable<User> ApplySort(IQueryable<User> query, string sort)
    {
        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return (field, descending) switch
        {
            ("email", false) => query.OrderBy(u => u.Email),
            ("email", true) => query.OrderByDescending(u => u.Email),
            ("displayName", false) => query.OrderBy(u => u.DisplayName),
            ("displayName", true) => query.OrderByDescending(u => u.DisplayName),
            ("status", false) => query.OrderBy(u => u.Status),
            ("status", true) => query.OrderByDescending(u => u.Status),
            (_, true) => query.OrderByDescending(u => u.CreatedAtUtc),
            _ => query.OrderBy(u => u.CreatedAtUtc),
        };
    }
}
