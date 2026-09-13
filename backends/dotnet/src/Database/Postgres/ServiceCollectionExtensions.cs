using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Features.Identity.Persistence.Repositories;
using StackBraid.Shared.Persistence;

namespace StackBraid.Database.Postgres;

/// <summary>
/// Wires <see cref="IdentityDbContext"/> to a real Postgres database. The
/// only file in the .NET backend allowed to say "Npgsql" or "Postgres" —
/// see <c>docs/STRUCTURE.md</c>. Swapping providers means writing a sibling
/// <c>Database/SqlServer</c> or <c>Database/MySql</c> with this same shape;
/// nothing above this layer changes.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }

    /// <summary>Runs pending migrations from empty to current — no manual step, called once at startup.</summary>
    public static async Task MigratePostgresDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
