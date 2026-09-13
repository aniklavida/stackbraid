using Microsoft.Extensions.DependencyInjection;
using StackBraid.Database.Postgres;
using StackBraid.Shared.Security;

namespace StackBraid.Features.Identity.IntegrationTests;

/// <summary>
/// Migrates a real local Postgres — no Docker, no Testcontainers — once per
/// test collection. The connection string comes from an environment
/// variable rather than being started by the test process itself: run
/// <c>backends/dotnet/scripts/start-local-postgres.sh</c> first (or point
/// the variable at any other empty Postgres database, including a CI
/// service container) — see this project's README.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvVar = "STACKBRAID_TEST_POSTGRES_CONNECTION_STRING";

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvVar} is not set. Run `./scripts/start-local-postgres.sh` from " +
                "backends/dotnet and export its printed connection string, or point the variable at any " +
                "other empty Postgres database (see this project's README).");
        }

        var services = new ServiceCollection();
        services.AddPostgresPersistence(connectionString);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        Services = services.BuildServiceProvider();

        await Services.MigratePostgresDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        (Services as IDisposable)?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "Postgres";
}
