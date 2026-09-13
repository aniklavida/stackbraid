using System.Text.RegularExpressions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackBraid.Features.Identity.Application.Commands;
using StackBraid.Features.Identity.Application.Queries;
using StackBraid.Features.Identity.Contracts.Requests;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Features.Identity.Endpoints.Authorization;
using StackBraid.Shared.Localization;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/users").WithTags("Users").RequireAuthorization();

        group.MapGet("", ListUsersAsync).RequirePermission("users:read");
        group.MapGet("/{userId:guid}", GetUserAsync).RequirePermission("users:read");
        group.MapPatch("/{userId:guid}", UpdateUserAsync).RequirePermission("users:write");
        group.MapPost("/{userId:guid}/deactivate", DeactivateUserAsync).RequirePermission("users:write");

        return app;
    }

    private static readonly Regex SortPattern = new(@"^-?(email|displayName|createdAt|status)$", RegexOptions.Compiled);

    private static async Task<IResult> ListUsersAsync(
        int? page,
        int? pageSize,
        string? sort,
        string? search,
        string? status,
        Guid? roleId,
        ISender sender,
        IAppLocalizer localizer,
        HttpContext context)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? 20;
        var resolvedSort = sort ?? "-createdAt";

        var fieldErrors = new Dictionary<string, string[]>();
        if (resolvedPage < 1)
        {
            fieldErrors["page"] = ["validation.page.minimum"];
        }

        if (resolvedPageSize is < 1 or > 100)
        {
            fieldErrors["pageSize"] = ["validation.page_size.range"];
        }

        if (!SortPattern.IsMatch(resolvedSort))
        {
            fieldErrors["sort"] = ["validation.sort.invalid"];
        }

        UserStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status))
        {
            // Query-string enum binding is case-sensitive to the C# member name by
            // default (`Active`), but the contract's UserStatus values are lower-case
            // (`active`) — parsed here, case-insensitively, instead of at the parameter.
            if (Enum.TryParse<UserStatus>(status, ignoreCase: true, out var parsed))
            {
                parsedStatus = parsed;
            }
            else
            {
                fieldErrors["status"] = ["validation.status.invalid"];
            }
        }

        if (fieldErrors.Count > 0)
        {
            return AppError.Validation("IDENTITY.VALIDATION_FAILED", "identity.validation_failed", fieldErrors)
                .ToProblemResult(context, localizer);
        }

        var result = await sender.Send(new ListUsersQuery(resolvedPage, resolvedPageSize, resolvedSort, search, parsedStatus, roleId));

        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> GetUserAsync(Guid userId, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new GetUserQuery(userId));
        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> UpdateUserAsync(Guid userId, UpdateUserRequest request, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new UpdateUserCommand(userId, request.DisplayName, request.Email));
        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> DeactivateUserAsync(Guid userId, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new DeactivateUserCommand(userId));
        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }
}
