using System.Security.Claims;
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

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/auth").WithTags("Auth");

        // register/login/refresh carry `security: []` in the contract — public, no bearer token needed.
        group.MapPost("/register", RegisterAsync).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);
        group.MapPost("/login", LoginAsync).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);
        group.MapPost("/refresh", RefreshAsync).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new RegisterUserCommand(request.Email, request.Password, request.DisplayName));
        return result.Match(
            dto => Results.Created($"/v1/users/{dto.Id}", dto),
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password));
        return result.Match(
            tokenPair =>
            {
                RefreshTokenCookie.Set(context, tokenPair.RefreshToken);
                return Results.Ok(tokenPair);
            },
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest? request, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var rawToken = request?.RefreshToken ?? RefreshTokenCookie.Read(context);
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return AppError.Unauthorized("IDENTITY.REFRESH_TOKEN_INVALID", "identity.refresh_token_invalid").ToProblemResult(context, localizer);
        }

        var result = await sender.Send(new RefreshTokenCommand(rawToken));
        return result.Match(
            tokenPair =>
            {
                RefreshTokenCookie.Set(context, tokenPair.RefreshToken);
                return Results.Ok(tokenPair);
            },
            error => error.ToProblemResult(context, localizer));
    }

    private static async Task<IResult> LogoutAsync(RefreshRequest? request, ISender sender, HttpContext context)
    {
        var rawToken = request?.RefreshToken ?? RefreshTokenCookie.Read(context);
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await sender.Send(new LogoutCommand(rawToken));
        }

        RefreshTokenCookie.Clear(context);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(ClaimsPrincipal user, ISender sender, IAppLocalizer localizer, HttpContext context)
    {
        var userId = user.GetUserId();
        var result = await sender.Send(new GetUserQuery(userId));
        return result.Match(
            dto => Results.Ok(dto),
            error => error.ToProblemResult(context, localizer));
    }
}
