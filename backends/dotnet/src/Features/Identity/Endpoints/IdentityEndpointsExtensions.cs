using Microsoft.AspNetCore.Routing;

namespace StackBraid.Features.Identity.Endpoints;

public static class IdentityEndpointsExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapUsersEndpoints();
        app.MapAuditEndpoints();
        app.MapRolesEndpoints();
        app.MapNotificationEndpoints();
        return app;
    }
}
