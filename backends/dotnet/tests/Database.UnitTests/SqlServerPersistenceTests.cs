using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Database.SqlServer;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.UnitTests;

/// <summary>
/// Proves the SQL Server provider's DI wiring and its query translation
/// without a live database. Standing up SQL Server is out of scope for a local
/// run; the live-database legs for this provider run in CI as a service
/// container.
/// </summary>
public class SqlServerPersistenceTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSqlServerPersistence("Server=localhost,1433;Database=stackbraid_test;User Id=sa;Password=ChangeMe!123;TrustServerCertificate=True");
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSqlServerPersistence_resolves_the_identity_context_against_the_sql_server_provider()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        context.Database.ProviderName.ShouldBe("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact]
    public void AddSqlServerPersistence_registers_the_persisted_job_store()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IJobStore>().ShouldBeOfType<SqlServerJobStore>();
    }

    [Fact]
    public void The_users_query_translates_to_sql_server_specific_sql_without_a_live_database()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var sql = context.Users.Where(u => u.DisplayName == "Ada").ToQueryString();

        sql.ShouldContain("[users]");
    }
}
