using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Database.Postgres;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.UnitTests;

/// <summary>
/// Proves the Postgres provider's DI wiring and its query translation without a
/// live database. The real, live-database checks for this provider are the
/// integration and conformance suites; this file only closes the same gap for
/// every provider, uniformly.
/// </summary>
public class PostgresPersistenceTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddPostgresPersistence("Host=localhost;Database=stackbraid_test;Username=postgres;Password=postgres");
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddPostgresPersistence_resolves_the_identity_context_against_the_postgres_provider()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        context.Database.ProviderName.ShouldBe("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void AddPostgresPersistence_registers_the_persisted_job_store()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IJobStore>().ShouldBeOfType<PostgresJobStore>();
    }

    [Fact]
    public void The_users_query_translates_to_postgres_specific_sql_without_a_live_database()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var sql = context.Users.Where(u => u.DisplayName == "Ada").ToQueryString();

        sql.ShouldContain("\"DisplayName\"");
    }
}
