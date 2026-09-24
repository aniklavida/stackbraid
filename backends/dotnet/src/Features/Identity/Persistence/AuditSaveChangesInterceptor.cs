using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Shared.Auditing;
using StackBraid.Shared.Web;
using System.Security.Claims;

namespace StackBraid.Features.Identity.Persistence;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

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
        if (context is null)
        {
            return;
        }

        var auditLog = ((IInfrastructure<IServiceProvider>)context).Instance.GetService<IAuditLog>();
        if (auditLog is null)
        {
            return;
        }

        var actorId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId() ?? "unknown";
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<User>())
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

            auditLog.Add(new AuditEntry(
                Guid.NewGuid(),
                nameof(User),
                entry.Entity.Id,
                action,
                actorId,
                correlationId,
                now,
                null));
        }
    }
}
