using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackBraid.Database.Postgres;
using StackBraid.Shared;

// A worker entry point with no HTTP surface: it shares the composition root
// (AddShared + AddPostgresPersistence) with the API, so it drains the exact
// same persisted job queue from the same Postgres. Point
// ConnectionStrings:Postgres at the database the API uses — via
// appsettings.json, an environment variable, or
// STACKBRAID_POSTGRES_CONNECTION_STRING — and it will pick up jobs the API
// queued, including any that were queued before this process started.
var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("STACKBRAID_POSTGRES_CONNECTION_STRING");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Host=localhost;Port=5432;Database=stackbraid;Username=postgres;Password=postgres";
}

builder.Services.AddShared(builder.Configuration);
builder.Services.AddPostgresPersistence(connectionString);

var host = builder.Build();

// Migrate/ensure schema before draining anything, so a worker started before
// the API still finds the job table it expects.
await host.Services.MigratePostgresDatabaseAsync();

await host.RunAsync();
