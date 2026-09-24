using Microsoft.EntityFrameworkCore;
using StackBraid.Shared.Auditing;

namespace StackBraid.Features.Identity.Persistence;

public sealed class AuditLogModel
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Details { get; set; }
}

public sealed class IdentityAuditLog : IAuditLog
{
    private readonly IdentityDbContext _context;

    public IdentityAuditLog(IdentityDbContext context)
    {
        _context = context;
    }

    public void Add(AuditEntry entry)
    {
        _context.AuditLogs.Add(new AuditLogModel
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Action = entry.Action,
            ActorId = entry.ActorId,
            CorrelationId = entry.CorrelationId,
            OccurredAtUtc = entry.OccurredAtUtc,
            Details = entry.Details,
        });
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.EntityId == entityId)
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new AuditEntry(
            row.Id,
            row.EntityType,
            row.EntityId,
            row.Action,
            row.ActorId,
            row.CorrelationId,
            row.OccurredAtUtc,
            row.Details)).ToList();
    }
}
