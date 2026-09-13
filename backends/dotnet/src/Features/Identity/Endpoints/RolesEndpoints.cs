using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Application.Queries;
using StackBraid.Features.Identity.Contracts.Requests;
using StackBraid.Features.Identity.Endpoints.Authorization;
using StackBraid.Shared.Localization;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Endpoints;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/roles", ListRolesAsync)
            .WithTags("Roles")
            .RequireAuthorization()
            .RequirePermission("roles:read");

        app.MapPost("/v1/users/{userId:guid}/roles", AssignRoleAsync)
            .WithTags("Roles")
            .RequireAuthorization()
            .RequirePermission("roles:write");

        app.MapDelete("/v1/users/{userId:guid}/roles/{roleId:guid}", RevokeRoleAsync)
            .WithTags("Roles")
            .RequireAuthorization()
            .RequirePermission("roles:write");

        return app;
    }

    private static async Task<IResult> ListRolesAsync(ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new ListRolesQuery());
        return result.Match(
            roles => Results.Ok(roles),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> AssignRoleAsync(Guid userId, AssignRoleRequest request, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new AssignRoleCommand(userId, request.RoleId));
        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> RevokeRoleAsync(Guid userId, Guid roleId, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new RevokeRoleCommand(userId, roleId));
        return result.Match(
            () => Results.NoContent(),
            error => error.ToProblemResult(context, localizer));
    }
}
