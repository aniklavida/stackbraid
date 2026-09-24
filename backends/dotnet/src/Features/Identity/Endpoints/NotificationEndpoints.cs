using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackBraid.Features.Identity.Endpoints.Authorization;
using StackBraid.Shared.Localization;
using StackBraid.Shared.Notifications;
using StackBraid.Shared.Web;

namespace StackBraid.Features.Identity.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var devices = app.MapGroup("/v1/devices").WithTags("Devices").RequireAuthorization();
        devices.MapPost("", RegisterDeviceTokenAsync);
        devices.MapPatch("/{deviceId:guid}", RefreshDeviceTokenAsync);
        devices.MapDelete("/{deviceId:guid}", RevokeDeviceTokenAsync);

        var notifications = app.MapGroup("/v1/notifications").WithTags("Notifications").RequireAuthorization();
        notifications.MapGet("", ListNotificationsAsync);
        return app;
    }

    private static async Task<IResult> RegisterDeviceTokenAsync(
        RegisterDeviceTokenRequest request,
        HttpContext context,
        IDeviceTokenStore store,
        IAppLocalizer localizer)
    {
        var validation = Validate(request.Token, request.Platform, request.AppVersion);
        if (validation is not null)
        {
            return validation.ToProblemResult(context, localizer);
        }

        try
        {
            var device = await store.RegisterAsync(context.User.GetUserId(), request.Token!, request.Platform!, request.AppVersion ?? string.Empty);
            return Results.Created($"/v1/devices/{device.Id}", ToResponse(device));
        }
        catch (InvalidOperationException)
        {
            return AppError.Conflict("DEVICE.TOKEN_ALREADY_REGISTERED", "notifications.token_already_registered")
                .ToProblemResult(context, localizer);
        }
    }

    private static async Task<IResult> RefreshDeviceTokenAsync(
        Guid deviceId,
        RefreshDeviceTokenRequest request,
        HttpContext context,
        IDeviceTokenStore store,
        IAppLocalizer localizer)
    {
        var validation = Validate(request.Token, null, null);
        if (validation is not null)
        {
            return validation.ToProblemResult(context, localizer);
        }

        var device = await store.RefreshAsync(context.User.GetUserId(), deviceId, request.Token!);
        return device is null
            ? AppError.NotFound("DEVICE.NOT_FOUND", "notifications.device_not_found").ToProblemResult(context, localizer)
            : Results.Ok(ToResponse(device));
    }

    private static async Task<IResult> RevokeDeviceTokenAsync(
        Guid deviceId,
        HttpContext context,
        IDeviceTokenStore store,
        IAppLocalizer localizer)
    {
        return await store.RevokeAsync(context.User.GetUserId(), deviceId)
            ? Results.NoContent()
            : AppError.NotFound("DEVICE.NOT_FOUND", "notifications.device_not_found").ToProblemResult(context, localizer);
    }

    private static async Task<IResult> ListNotificationsAsync(
        int? page,
        int? pageSize,
        HttpContext context,
        INotificationStore store,
        IAppLocalizer localizer)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? 20;
        var validation = resolvedPage < 1 || resolvedPageSize is < 1 or > 100
            ? AppError.Validation("IDENTITY.VALIDATION_FAILED", "identity.validation_failed", new Dictionary<string, string[]>
            {
                ["page"] = ["validation.page.minimum"],
                ["pageSize"] = ["validation.page_size.range"],
            })
            : null;
        if (validation is not null)
        {
            return validation.ToProblemResult(context, localizer);
        }

        var totalItems = await store.CountAsync(context.User.GetUserId());
        var items = await store.ListAsync(context.User.GetUserId(), resolvedPage, resolvedPageSize);
        return Results.Ok(new
        {
            page = resolvedPage,
            pageSize = resolvedPageSize,
            totalItems,
            totalPages = (int)Math.Ceiling(totalItems / (double)resolvedPageSize),
            items = items.Select(ToResponse),
        });
    }

    private static AppError? Validate(string? token, string? platform, string? appVersion)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
        {
            errors["token"] = ["validation.token.invalid"];
        }

        if (platform is not null && platform is not ("ios" or "android" or "web"))
        {
            errors["platform"] = ["validation.platform.invalid"];
        }

        if (appVersion is not null && appVersion.Length > 50)
        {
            errors["appVersion"] = ["validation.app_version.invalid"];
        }

        return errors.Count == 0 ? null : AppError.Validation("IDENTITY.VALIDATION_FAILED", "identity.validation_failed", errors);
    }

    private static object ToResponse(DeviceTokenRegistration device) => new
    {
        id = device.Id,
        platform = device.Platform,
        appVersion = device.AppVersion,
        registeredAt = device.RegisteredAtUtc,
        updatedAt = device.UpdatedAtUtc,
    };

    private static object ToResponse(StoredNotification notification) => new
    {
        id = notification.Id,
        title = notification.Title,
        body = notification.Body,
        read = notification.Read,
        createdAt = notification.CreatedAtUtc,
    };

    public sealed record RegisterDeviceTokenRequest(string? Token, string? Platform, string? AppVersion);

    public sealed record RefreshDeviceTokenRequest(string? Token);
}
