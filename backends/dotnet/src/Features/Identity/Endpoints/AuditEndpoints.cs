using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackBraid.Features.Identity.Contracts.Dtos;
using StackBraid.Features.Identity.Endpoints.Authorization;
using StackBraid.Shared.Auditing;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/audit", async (Guid userId, IAuditLog auditLog, CancellationToken cancellationToken) =>
        {
            var entries = await auditLog.ListAsync(userId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new AuditPageDto(entries.Select(entry => new AuditEntryDto(
                entry.Id,
                entry.EntityType,
                entry.EntityId,
                entry.Action,
                entry.ActorId,
                entry.CorrelationId,
                entry.OccurredAtUtc,
                entry.Details)).ToList()));
        }).WithTags("Audit").RequireAuthorization().RequirePermission("users:read");
        return app;
    }
}
