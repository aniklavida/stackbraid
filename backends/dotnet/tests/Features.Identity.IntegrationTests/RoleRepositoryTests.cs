using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class RoleRepositoryTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public RoleRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_round_trips_a_role()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var role = Role.Create($"role-{Guid.NewGuid():N}", "A test role.", ["things:read", "things:write"]);
        await repository.AddAsync(role);
        await unitOfWork.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(role.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe(role.Name);
        loaded.Description.ShouldBe("A test role.");
        loaded.Permissions.ShouldBe(["things:read", "things:write"], ignoreOrder: true);
    }

    [Fact]
    public async Task ListAllAsync_includes_a_newly_added_role()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var role = Role.Create($"role-{Guid.NewGuid():N}", null, []);
        await repository.AddAsync(role);
        await unitOfWork.SaveChangesAsync();

        var all = await repository.ListAllAsync();

        all.ShouldContain(r => r.Id == role.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_an_unknown_id()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        loaded.ShouldBeNull();
    }
}
