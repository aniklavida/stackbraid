using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackBraid.Features.Identity.Persistence;

namespace StackBraid.Host.HealthChecks;

/// <summary>Readiness, not liveness: the process can be up while the database is unreachable, and that distinction matters to an orchestrator.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IdentityDbContext _context;

    public DatabaseHealthCheck(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return canConnect
            ? HealthCheckResult.Healthy("Postgres is reachable.")
            : HealthCheckResult.Unhealthy("Postgres is not reachable.");
    }
}
