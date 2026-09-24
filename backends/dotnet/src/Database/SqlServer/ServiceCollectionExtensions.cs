using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Features.Identity.Persistence.Repositories;
using StackBraid.Shared.Auditing;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Persistence;

namespace StackBraid.Database.SqlServer;

/// <summary>
/// Wires <see cref="IdentityDbContext"/> to a real SQL Server database. The
/// only file in the .NET backend allowed to say "SqlServer" or name the
/// Microsoft SQL Server driver — see <c>docs/STRUCTURE.md</c>. A sibling of
/// <c>Database/Postgres</c> and <c>Database/MySql</c> with this same shape;
/// nothing above this layer changes.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddHttpContextAccessor();
        services.AddDbContext<IdentityDbContext>((provider, options) =>
            options
                .UseSqlServer(connectionString, sqlServer =>
                    sqlServer.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName))
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
        services.AddSingleton(new SqlServerJobStore(connectionString));
        services.AddSingleton<IJobStore>(sp => sp.GetRequiredService<SqlServerJobStore>());

        return services;
    }

    /// <summary>Runs pending migrations from empty to current, then ensures the job table — no manual step, called once at startup.</summary>
    public static async Task MigrateSqlServerDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var jobStore = scope.ServiceProvider.GetRequiredService<SqlServerJobStore>();
        await jobStore.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
    }
}
