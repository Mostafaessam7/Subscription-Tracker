using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SubscriptionTracker.Api.Hubs;

/// <summary>
/// Pushes in-app notifications live to a connected user. Each connection joins a group named after the caller's
/// user id (from the JWT `sub`/NameIdentifier claim), so NotificationPublisher can target a specific recipient
/// without tracking connection ids itself - matches how the JWT already scopes everything else per-user.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(string userId) => $"user:{userId}";
}
