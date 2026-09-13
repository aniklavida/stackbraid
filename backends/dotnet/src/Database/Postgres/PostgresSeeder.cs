using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Shared.Security;

namespace StackBraid.Database.Postgres;

/// <summary>
/// Seeds exactly what a fresh database needs to be usable: two roles and
/// two accounts. Idempotent — checked by presence, so running it against an
/// already-seeded database (every ordinary startup, after the first) is a
/// no-op rather than a duplicate-key error.
/// </summary>
public static class PostgresSeeder
{
    public const string AdminEmail = "admin@stackbraid.local";
    public const string StandardUserEmail = "user@stackbraid.local";

    /// <summary>
    /// Only used for the seeded accounts in a fresh, non-production
    /// database — see <c>backends/dotnet/README.md</c>. Never used for an
    /// account a real user registers.
    /// </summary>
    public const string SeedPassword = "ChangeMe!123";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var adminEmail = Email.Create(AdminEmail);
        if (await context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken).ConfigureAwait(false))
        {
            return; // Already seeded.
        }

        var now = DateTime.UtcNow;

        var adminRole = Role.Create("admin", "Full administrative access.", ["users:read", "users:write", "roles:read", "roles:write"]);
        var userRole = Role.Create("user", "An ordinary authenticated account.", ["users:read:self"]);
        context.Roles.AddRange(adminRole, userRole);

        var admin = User.Register(Email.Create(AdminEmail), hasher.Hash(SeedPassword), "Administrator", now);
        admin.AssignRole(adminRole, now);

        var standardUser = User.Register(Email.Create(StandardUserEmail), hasher.Hash(SeedPassword), "Standard User", now);
        standardUser.AssignRole(userRole, now);

        context.Users.AddRange(admin, standardUser);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
