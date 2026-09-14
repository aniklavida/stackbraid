using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StackBraid.Host.Realtime;

/// <summary>
/// <c>/v1/hubs/notifications</c> — a per-user stream carrying
/// <c>UserDeactivatedMessage</c> and <c>UserRoleChangedMessage</c>, per
/// <c>contract/openapi.yaml</c>'s <c>x-realtime-channels</c> section. A
/// client connects with its access token (query string — a browser
/// WebSocket handshake carries no custom headers) and is placed in group
/// <c>user:{userId}</c> the moment the connection is authenticated; nothing
/// the client sends decides which group it joins.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(string userId) => $"user:{userId}";
}
