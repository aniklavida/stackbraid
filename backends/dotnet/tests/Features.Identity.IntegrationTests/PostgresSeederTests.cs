using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Database.Postgres;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Features.Identity.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class PostgresSeederTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public PostgresSeederTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedAsync_creates_two_roles_and_two_users()
    {
        await PostgresSeeder.SeedAsync(_fixture.Services);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var roleNames = await context.Roles.Select(r => r.Name).ToListAsync();
        roleNames.ShouldContain("admin");
        roleNames.ShouldContain("user");

        var userEmails = await context.Users.Select(u => EF.Property<string>(u, "Email")).ToListAsync();
        userEmails.ShouldContain(PostgresSeeder.AdminEmail);
        userEmails.ShouldContain(PostgresSeeder.StandardUserEmail);
    }

    [Fact]
    public async Task SeedAsync_gives_the_admin_the_admin_role()
    {
        await PostgresSeeder.SeedAsync(_fixture.Services);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var adminEmail = Email.Create(PostgresSeeder.AdminEmail);

        var admin = await context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleAsync(u => u.Email == adminEmail);

        admin.Roles.Select(r => r.Name).ShouldContain("admin");
    }

    [Fact]
    public async Task SeedAsync_run_twice_does_not_duplicate_data()
    {
        await PostgresSeeder.SeedAsync(_fixture.Services);
        await PostgresSeeder.SeedAsync(_fixture.Services);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var adminEmail = Email.Create(PostgresSeeder.AdminEmail);

        (await context.Roles.CountAsync(r => r.Name == "admin" || r.Name == "user")).ShouldBe(2);
        (await context.Users.CountAsync(u => u.Email == adminEmail)).ShouldBe(1);
    }
}
