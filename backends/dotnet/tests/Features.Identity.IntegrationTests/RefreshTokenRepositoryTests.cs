using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Domain.Repositories;
using StackBraid.Features.Identity.Domain.ValueObjects;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class RefreshTokenRepositoryTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public RefreshTokenRepositoryTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> CreateUserAsync(IServiceScope scope)
    {
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = User.Register(Email.Create($"user-{Guid.NewGuid():N}@test.example"), "hashed", "Token Owner", DateTime.UtcNow);
        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task Issue_then_GetByTokenHashAsync_round_trips_a_token()
    {
        using var scope = _fixture.Services.CreateScope();
        var userId = await CreateUserAsync(scope);
        var tokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tokenHash = $"hash-{Guid.NewGuid():N}";
        var token = RefreshToken.Issue(userId, tokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        await tokens.AddAsync(token);
        await unitOfWork.SaveChangesAsync();

        var loaded = await tokens.GetByTokenHashAsync(tokenHash);

        loaded.ShouldNotBeNull();
        loaded.UserId.ShouldBe(userId);
        loaded.IsActive(DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public async Task Revoke_persists_and_the_token_is_no_longer_active()
    {
        using var scope = _fixture.Services.CreateScope();
        var userId = await CreateUserAsync(scope);
        var tokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tokenHash = $"hash-{Guid.NewGuid():N}";
        var token = RefreshToken.Issue(userId, tokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);
        await tokens.AddAsync(token);
        await unitOfWork.SaveChangesAsync();

        token.Revoke(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync();

        using var freshScope = _fixture.Services.CreateScope();
        var reloaded = await freshScope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>().GetByTokenHashAsync(tokenHash);

        reloaded!.IsActive(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public async Task GetByTokenHashAsync_returns_null_for_an_unknown_hash()
    {
        using var scope = _fixture.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var loaded = await tokens.GetByTokenHashAsync($"no-such-hash-{Guid.NewGuid():N}");

        loaded.ShouldBeNull();
    }
}
