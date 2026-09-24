using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Features.Identity.Persistence.Repositories;
using StackBraid.Shared.Auditing;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Persistence;

namespace StackBraid.Database.MySql;

/// <summary>
/// Wires <see cref="IdentityDbContext"/> to a real MySQL database through
/// Pomelo. The only file in the .NET backend allowed to say "MySql" or name
/// the MySQL driver — see <c>docs/STRUCTURE.md</c>. A sibling of
/// <c>Database/Postgres</c> and <c>Database/SqlServer</c> with this same
/// shape; nothing above this layer changes.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The server version an ordinary local MySQL 8 deployment reports. Callers
    /// that need a different version pass it explicitly through the overload
    /// below rather than the composition root probing a live server at startup.
    /// </summary>
    public static readonly ServerVersion DefaultServerVersion = new MySqlServerVersion(new Version(8, 0, 36));

    public static IServiceCollection AddMySqlPersistence(this IServiceCollection services, string connectionString) =>
        AddMySqlPersistence(services, connectionString, DefaultServerVersion);

    public static IServiceCollection AddMySqlPersistence(this IServiceCollection services, string connectionString, ServerVersion serverVersion)
    {
        services.AddHttpContextAccessor();
        services.AddDbContext<IdentityDbContext>((provider, options) =>
            options
                .UseMySql(connectionString, serverVersion, mySql =>
                    mySql.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName))
                .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IAuditLog, IdentityAuditLog>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // The persisted job store replaces the in-memory default registered by
        // AddPersistentJobs (registration order decides), which is what makes a
        // queued job outlive the process that queued it.
        services.AddSingleton(new MySqlJobStore(connectionString));
        services.AddSingleton<IJobStore>(sp => sp.GetRequiredService<MySqlJobStore>());

        return services;
    }

    /// <summary>Runs pending migrations from empty to current, then ensures the job table — no manual step, called once at startup.</summary>
    public static async Task MigrateMySqlDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var jobStore = scope.ServiceProvider.GetRequiredService<MySqlJobStore>();
        await jobStore.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
    }
}
