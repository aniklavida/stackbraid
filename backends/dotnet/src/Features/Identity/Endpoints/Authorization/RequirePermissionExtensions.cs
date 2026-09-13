using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace StackBraid.Features.Identity.Endpoints.Authorization;

/// <summary>
/// The role-based authorization this feature actually enforces: an
/// endpoint names the permission code it needs, and the caller's access
/// token must carry that <see cref="PermissionClaim"/> — see
/// <c>contract/openapi.yaml</c>'s per-endpoint 403 responses.
/// </summary>
public static class RequirePermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(policy => policy.RequireClaim(PermissionClaim.Type, permission));
        return builder;
    }
}
