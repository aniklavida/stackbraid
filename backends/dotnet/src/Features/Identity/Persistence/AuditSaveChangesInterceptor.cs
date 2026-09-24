using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Shared.Auditing;
using StackBraid.Shared.Web;
using System.Security.Claims;

namespace StackBraid.Features.Identity.Persistence;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Writes go straight into the same DbContext instance handed to this
    // interceptor via eventData.Context, as an IdentityDbContext, rather
    // than through IAuditLog: IAuditLog's implementation needs an
    // IdentityDbContext, and this interceptor is itself constructed while
    // building that same IdentityDbContext's options (AddDbContext resolves
    // registered interceptors from the app's DI container before the
    // context exists) — injecting IAuditLog here creates a circular
    // dependency that deadlocks DI resolution. Reading audit history still
    // goes through IAuditLog elsewhere, where no such cycle exists.
    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        WriteEntries(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void WriteEntries(DbContext? context)
    {
        if (context is not IdentityDbContext identityContext)
        {
            return;
        }

        var actorId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId() ?? "unknown";
        var now = DateTime.UtcNow;

        // Materialized eagerly: adding to AuditLogs below mutates the same
        // change tracker this enumerates, which invalidates a live iterator.
        foreach (var entry in identityContext.ChangeTracker.Entries<User>().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Deleted => "deleted",
                _ when entry.Property(nameof(User.DeletedAtUtc)).IsModified && entry.CurrentValues[nameof(User.DeletedAtUtc)] is not null => "deleted",
                _ when entry.Property(nameof(User.DeletedAtUtc)).IsModified => "restored",
                _ => "updated",
            };

            identityContext.AuditLogs.Add(new AuditLogModel
            {
                Id = Guid.NewGuid(),
                EntityType = nameof(User),
                EntityId = entry.Entity.Id,
                Action = action,
                ActorId = actorId,
                CorrelationId = correlationId,
                OccurredAtUtc = now,
                Details = null,
            });
        }
    }
}
