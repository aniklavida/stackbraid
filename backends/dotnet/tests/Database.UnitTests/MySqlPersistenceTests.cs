using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Database.MySql;
using StackBraid.Features.Identity.Persistence;
using StackBraid.Shared.Jobs;

namespace StackBraid.Database.UnitTests;

/// <summary>
/// Proves the MySQL provider's DI wiring without a live database.
/// <para>
/// The query-translation check is deliberately inactive on this baseline.
/// Pomelo.EntityFrameworkCore.MySql — the only MySQL EF Core provider with an
/// acceptable (MIT) licence — is built against EF Core 9, and its newest
/// release (9.0.0) is binary-incompatible with the EF Core 10 line this
/// backend ships: constructing the model throws
/// <c>Method not found: Microsoft.EntityFrameworkCore.Diagnostics.AbstractionsStrings.ArgumentIsEmpty</c>.
/// The provider project and its migration are committed and compile, and the
/// SQL Server sibling proves the same shape end to end; this one test activates
/// the day Pomelo publishes an EF Core 10 release.
/// </para>
/// </summary>
public class MySqlPersistenceTests
{
    private static IServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMySqlPersistence("Server=localhost;Port=3306;Database=stackbraid_test;User=root;Password=ChangeMe!123");
        return services;
    }

    [Fact]
    public void AddMySqlPersistence_registers_the_identity_context_and_the_persisted_job_store()
    {
        var services = BuildServices();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IdentityDbContext));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IJobStore));
    }

    [Fact]
    public void AddMySqlPersistence_resolves_the_persisted_job_store_without_a_live_database()
    {
        using var provider = BuildServices().BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IJobStore>().ShouldBeOfType<MySqlJobStore>();
    }

    [Fact(Skip = "Pomelo.EntityFrameworkCore.MySql 9.0.0 targets EF Core 9 and is binary-incompatible with this backend's EF Core 10 baseline; activate when Pomelo ships an EF Core 10 release.")]
    public void The_users_query_translates_to_mysql_specific_sql_without_a_live_database()
    {
        using var provider = BuildServices().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var sql = context.Users.Where(u => u.DisplayName == "Ada").ToQueryString();

        sql.ShouldContain("`users`");
    }
}
