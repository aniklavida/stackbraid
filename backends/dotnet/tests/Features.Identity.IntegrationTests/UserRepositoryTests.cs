using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class UserRepositoryTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public UserRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static Email UniqueEmail() => Email.Create($"user-{Guid.NewGuid():N}@test.example");

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_round_trips_a_user()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var email = UniqueEmail();
        var user = User.Register(email, "hashed", "Ada Lovelace", DateTime.UtcNow);
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded.Email.ShouldBe(email);
        loaded.DisplayName.ShouldBe("Ada Lovelace");
        loaded.Status.ShouldBe(UserStatus.Active);
    }

    [Fact]
    public async Task GetByEmailAsync_finds_a_user_by_its_email()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var email = UniqueEmail();
        var user = User.Register(email, "hashed", "Grace Hopper", DateTime.UtcNow);
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        var loaded = await repository.GetByEmailAsync(email);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task ExistsByEmailAsync_is_true_only_after_the_user_is_saved()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var email = UniqueEmail();
        (await repository.ExistsByEmailAsync(email)).ShouldBeFalse();

        await repository.AddAsync(User.Register(email, "hashed", "Someone", DateTime.UtcNow));
        await unitOfWork.SaveChangesAsync();

        (await repository.ExistsByEmailAsync(email)).ShouldBeTrue();
    }

    [Fact]
    public async Task AssignRole_persists_the_link_and_is_visible_on_reload()
    {
        using var scope = _fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var role = Role.Create($"role-{Guid.NewGuid():N}", null, ["x:y"]);
        await roles.AddAsync(role);
        var user = User.Register(UniqueEmail(), "hashed", "Role Haver", DateTime.UtcNow);
        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        user.AssignRole(role, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync();

        using var freshScope = _fixture.Services.CreateScope();
        var freshUsers = freshScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var reloaded = await freshUsers.GetByIdAsync(user.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Roles.ShouldContain(r => r.Id == role.Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_search_term_case_insensitively()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var uniqueMarker = Guid.NewGuid().ToString("N")[..8];
        var user = User.Register(UniqueEmail(), "hashed", $"FindMe-{uniqueMarker}", DateTime.UtcNow);
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        var result = await repository.SearchAsync(new UserSearchQuery(
            Page: 1, PageSize: 20, Sort: "-createdAt", Search: $"findme-{uniqueMarker}".ToLowerInvariant(), Status: null, RoleId: null));

        result.Items.ShouldContain(u => u.Id == user.Id);
    }

    [Fact]
    public async Task SearchAsync_paginates_with_a_consistent_total_count()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var marker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 5; i++)
        {
            await repository.AddAsync(User.Register(UniqueEmail(), "hashed", $"Page-{marker}-{i}", DateTime.UtcNow));
        }

        await unitOfWork.SaveChangesAsync();

        var firstPage = await repository.SearchAsync(new UserSearchQuery(1, 2, "-createdAt", marker, null, null));
        var secondPage = await repository.SearchAsync(new UserSearchQuery(2, 2, "-createdAt", marker, null, null));

        firstPage.TotalCount.ShouldBe(5);
        firstPage.Items.Count.ShouldBe(2);
        secondPage.Items.Count.ShouldBe(2);
        firstPage.Items.Select(u => u.Id).Intersect(secondPage.Items.Select(u => u.Id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Deactivate_is_idempotent_and_persists()
    {
        using var scope = _fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = User.Register(UniqueEmail(), "hashed", "Deactivate Me", DateTime.UtcNow);
        await repository.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        user.Deactivate(DateTime.UtcNow);
        user.Deactivate(DateTime.UtcNow); // idempotent, per the contract
        await unitOfWork.SaveChangesAsync();

        using var freshScope = _fixture.Services.CreateScope();
        var reloaded = await freshScope.ServiceProvider.GetRequiredService<IUserRepository>().GetByIdIncludingDeletedAsync(user.Id);

        reloaded!.Status.ShouldBe(UserStatus.Inactive);
    }
}
