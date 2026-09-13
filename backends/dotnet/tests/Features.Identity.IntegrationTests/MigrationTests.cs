using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Features.Identity.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class MigrationTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public MigrationTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrations_bring_an_empty_database_fully_up_to_date()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var pending = await context.Database.GetPendingMigrationsAsync();

        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task Every_mapped_table_exists_after_migrating()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        foreach (var table in new[] { "users", "roles", "refresh_tokens", "user_roles" })
        {
            var exists = await context.Database
                .SqlQueryRaw<bool>(
                    "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = {0}) AS \"Value\"",
                    table)
                .SingleAsync();

            exists.ShouldBeTrue($"table '{table}' should exist after migrating");
        }
    }
}
