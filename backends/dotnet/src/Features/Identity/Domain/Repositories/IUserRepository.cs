using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.ValueObjects;

namespace StackBraid.Features.Identity.Domain.Repositories;

public sealed record UserSearchQuery(
    int Page,
    int PageSize,
    string Sort,
    string? Search,
    UserStatus? Status,
    Guid? RoleId,
    bool IncludeDeleted = false);

public sealed record UserSearchResult(IReadOnlyList<User> Items, int TotalCount);

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<UserSearchResult> SearchAsync(UserSearchQuery query, CancellationToken cancellationToken = default);
}
